using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;

namespace FindYourFood.Messaging;

public sealed class BrokerSettings
{
    public bool Enabled { get; init; }
    public required string Service { get; init; }
    public required string Host { get; init; }
    public required string User { get; init; }
    public required string VirtualHost { get; init; }
    public required string Prefix { get; init; }
    public required string PasswordPath { get; init; }
    public required string[] Bindings { get; init; }
    public int Port { get; init; }
    public bool Tls { get; init; }
    public string Exchange => Prefix + ".events";
    public string Queue => Prefix + "." + Service;

    public static BrokerSettings Read(IConfiguration configuration, IHostEnvironment environment, string service, string[] bindings)
    {
        var section = configuration.GetSection("Messaging");
        var settings = new BrokerSettings
        {
            Enabled = section.GetValue("Enabled", false), Service = service,
            Host = section["Host"] ?? "localhost", Port = section.GetValue("Port", 5672),
            User = section["User"] ?? "findyourfood", VirtualHost = section["VirtualHost"] ?? "findyourfood",
            Prefix = section["Prefix"] ?? "findyourfood", PasswordPath = section["PasswordPath"] ?? "",
            Tls = section.GetValue("Tls", false), Bindings = bindings
        };
        if (!settings.Enabled) return settings;
        if (string.IsNullOrWhiteSpace(settings.Host) || string.IsNullOrWhiteSpace(settings.User) ||
            string.IsNullOrWhiteSpace(settings.VirtualHost) || settings.Port is < 1 or > 65535 || settings.Prefix.Length is < 1 or > 60 ||
            settings.Prefix.Any(character => !(char.IsAsciiLetterOrDigit(character) || character is '.' or '-')) ||
            !File.Exists(settings.PasswordPath))
        {
            throw new InvalidOperationException("Configure messaging host, port, prefix and password file.");
        }
        if (!settings.Tls && (!environment.IsDevelopment() || settings.Host is not ("localhost" or "127.0.0.1" or "::1")))
        {
            throw new InvalidOperationException("Messaging requires TLS outside loopback development.");
        }
        return settings;
    }

    public ConnectionFactory Factory() => new()
    {
        HostName = Host, Port = Port, UserName = User, Password = File.ReadAllText(PasswordPath).Trim(),
        VirtualHost = VirtualHost, AutomaticRecoveryEnabled = false,
        RequestedHeartbeat = TimeSpan.FromSeconds(10), RequestedConnectionTimeout = TimeSpan.FromSeconds(5),
        Ssl = new SslOption { Enabled = Tls, ServerName = Host }
    };
}
