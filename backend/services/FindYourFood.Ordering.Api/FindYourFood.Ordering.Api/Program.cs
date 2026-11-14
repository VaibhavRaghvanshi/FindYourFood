using FindYourFood.Ordering.Application.Interfaces;
using FindYourFood.Ordering.Application.Services;
using FindYourFood.Ordering.Infrastructure.Http;
using FindYourFood.Ordering.Api.Workers;
using FindYourFood.Ordering.Api.HealthChecks;
using FindYourFood.Ordering.Infrastructure;
using FindYourFood.Hosting;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);
builder.AddFindYourFoodHosting("FindYourFood.Ordering");
builder.Services.AddControllers(options => options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true)
    .AddJsonOptions(options => options.JsonSerializerOptions.RespectRequiredConstructorParameters = true);
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks().AddCheck<OrderingReadiness>("ordering-sql", tags: ["ready"]);
builder.Services.AddOrdering(builder.Configuration.GetConnectionString("Ordering")
    ?? throw new InvalidOperationException("Configure ConnectionStrings:Ordering."));

TokenSettings.FromConfiguration(builder.Configuration).ConfigureBearer(builder.Services);
builder.Services.AddScoped<CustomerRequestFilter>();
var timeoutMinutes = builder.Configuration.GetValue("Ordering:PaymentTimeoutMinutes", 15);
if (timeoutMinutes is < 1 or > 60) throw new InvalidOperationException("Payment timeout must be between 1 and 60 minutes.");
builder.Services.AddSingleton(new CheckoutPolicy(TimeSpan.FromMinutes(timeoutMinutes)));
builder.Services.AddHttpClient<ICartCheckout, CartCheckoutClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Cart:BaseUrl"]!, UriKind.Absolute);
    client.Timeout = TimeSpan.FromSeconds(5);
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
builder.Services.AddHostedService<CheckoutTimeoutWorker>();
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

public sealed class OrderingHost;
