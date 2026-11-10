using FindYourFood.Customer.Api.HealthChecks;
using FindYourFood.Customer.Infrastructure;
using FindYourFood.Hosting;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);
builder.AddFindYourFoodHosting("FindYourFood.Customer");
builder.Services.AddControllers(options => options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true)
    .AddJsonOptions(options => options.JsonSerializerOptions.RespectRequiredConstructorParameters = true);
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks().AddCheck<CustomerReadiness>("customer-sql", tags: ["ready"]);
builder.Services.AddCustomer(builder.Configuration.GetConnectionString("Customer")
    ?? throw new InvalidOperationException("Configure ConnectionStrings:Customer."));
builder.Services.AddAuthorizationBuilder().AddPolicy("customer", policy => policy
    .RequireAuthenticatedUser().RequireAssertion(context =>
        !context.User.HasClaim(claim => claim.Type == "account_type") &&
        Guid.TryParse(context.User.FindFirst("sub")?.Value, out var id) && id != Guid.Empty));
TokenSettings.FromConfiguration(builder.Configuration).ConfigureBearer(builder.Services);
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

public sealed class CustomerHost;
