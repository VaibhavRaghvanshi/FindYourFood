using FindYourFood.Discovery.Api.HealthChecks;
using FindYourFood.Discovery.Infrastructure;
using FindYourFood.Discovery.Application.Interfaces;
using FindYourFood.Discovery.Infrastructure.Http;
using FindYourFood.Hosting;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);
builder.AddFindYourFoodHosting("FindYourFood.Discovery");
builder.Services.AddControllers(options => options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true)
    .AddJsonOptions(options => options.JsonSerializerOptions.RespectRequiredConstructorParameters = true);
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks().AddCheck<DiscoveryReadiness>("discovery-sql", tags: ["ready"]);
builder.Services.AddDiscovery(builder.Configuration.GetConnectionString("Discovery")
    ?? throw new InvalidOperationException("Configure ConnectionStrings:Discovery."));

TokenSettings.FromConfiguration(builder.Configuration).ConfigureBearer(builder.Services);
foreach (var service in new[] { "Provider", "Catalog", "Location" })
{
    builder.Services.AddHttpClient(service.ToLowerInvariant(), client =>
    {
        client.BaseAddress = new Uri(builder.Configuration[$"{service}:BaseUrl"]!, UriKind.Absolute);
        client.Timeout = TimeSpan.FromSeconds(3);
    }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
}
builder.Services.AddScoped<IPublishedDirectory>(services =>
{
    var clients = services.GetRequiredService<IHttpClientFactory>();
    return new PublishedDirectory(clients.CreateClient("provider"), clients.CreateClient("catalog"));
});
builder.Services.AddScoped<IDeliveryCoverage>(services =>
    new DeliveryCoverageClient(services.GetRequiredService<IHttpClientFactory>().CreateClient("location")));
var app = builder.Build();
app.UseFindYourFoodHosting();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapControllers();
}

app.Run();

public sealed class DiscoveryHost;
