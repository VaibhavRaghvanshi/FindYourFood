using FindYourFood.Cart.Api.Controllers;
using FindYourFood.Cart.Application.Interfaces;
using FindYourFood.Cart.Infrastructure.Http;
using FindYourFood.Cart.Api.HealthChecks;
using FindYourFood.Cart.Infrastructure;
using FindYourFood.Hosting;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);
builder.AddFindYourFoodHosting("FindYourFood.Cart");
builder.Services.AddControllers(options => options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true)
    .AddJsonOptions(options => options.JsonSerializerOptions.RespectRequiredConstructorParameters = true);
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks().AddCheck<CartReadiness>("cart-sql", tags: ["ready"]);
builder.Services.AddCart(builder.Configuration.GetConnectionString("Cart")
    ?? throw new InvalidOperationException("Configure ConnectionStrings:Cart."));

TokenSettings.FromConfiguration(builder.Configuration).ConfigureBearer(builder.Services);
builder.Services.AddScoped<CartCustomerFilter>();
foreach (var service in new[] { "Provider", "Catalog", "Location" })
{
    builder.Services.AddHttpClient(service.ToLowerInvariant(), client =>
    {
        client.BaseAddress = new Uri(builder.Configuration[$"{service}:BaseUrl"]!, UriKind.Absolute);
        client.Timeout = TimeSpan.FromSeconds(3);
    }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
}
builder.Services.AddScoped<ICommerceSource>(services =>
{
    var clients = services.GetRequiredService<IHttpClientFactory>();
    return new CommerceSource(clients.CreateClient("provider"), clients.CreateClient("catalog"),
        clients.CreateClient("location"), services.GetRequiredService<TimeProvider>());
});
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

public sealed class CartHost;
