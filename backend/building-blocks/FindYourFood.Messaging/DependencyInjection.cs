using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FindYourFood.Messaging;

public static class DependencyInjection
{
    public static IServiceCollection AddReliableMessaging<TDatabase>(this IServiceCollection services,
        IConfiguration configuration, IHostEnvironment environment, string service, params string[] bindings)
        where TDatabase : DbContext
    {
        services.AddSingleton(BrokerSettings.Read(configuration, environment, service, bindings));
        services.AddSingleton<RabbitPublisher>();
        services.AddScoped<IMessageStore>(provider => new SqlMessageStore(provider.GetRequiredService<TDatabase>()));
        services.AddHostedService<OutboxDispatcher>();
        services.AddHostedService<RabbitConsumer>();
        return services;
    }
}
