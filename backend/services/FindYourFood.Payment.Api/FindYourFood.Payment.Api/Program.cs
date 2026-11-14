using FindYourFood.Payment.Api.HealthChecks;
using FindYourFood.Payment.Infrastructure;
using FindYourFood.Contracts;
using FindYourFood.Messaging;
using FindYourFood.Payment.Infrastructure.Data;
using FindYourFood.Payment.Infrastructure.Http;
using FindYourFood.Payment.Infrastructure.Gateways;
using FindYourFood.Payment.Application.Interfaces;
using FindYourFood.Payment.Api.Workers;
using FindYourFood.Hosting;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);
builder.AddFindYourFoodHosting("FindYourFood.Payment");
builder.Services.AddControllers(options => options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true)
    .AddJsonOptions(options => options.JsonSerializerOptions.RespectRequiredConstructorParameters = true);
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks().AddCheck<PaymentReadiness>("payment-sql", tags: ["ready"]);
builder.Services.AddPayment(builder.Configuration.GetConnectionString("Payment")
    ?? throw new InvalidOperationException("Configure ConnectionStrings:Payment."));
builder.Services.AddScoped<CustomerRequestFilter>();
TokenSettings.FromConfiguration(builder.Configuration).ConfigureBearer(builder.Services);
builder.Services.AddHttpClient<IOrderPaymentAccess, OrderPaymentClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Ordering:BaseUrl"]!, UriKind.Absolute);
    client.Timeout = TimeSpan.FromSeconds(5);
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
builder.Services.AddSingleton<IPaymentGateway>(provider => builder.Environment.IsDevelopment()
    ? new DevelopmentPaymentGateway() : new UnconfiguredPaymentGateway());
builder.Services.AddSingleton<GatewaySignature>();
builder.Services.AddHostedService<PaymentRetryWorker>();
builder.Services.AddReliableMessaging<PaymentDbContext>(builder.Configuration, builder.Environment, "payment",
    "ordering.payment-requested.v1", "ordering.payment-cancelled.v1", "ordering.refund-requested.v1");
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

public sealed class PaymentHost;
