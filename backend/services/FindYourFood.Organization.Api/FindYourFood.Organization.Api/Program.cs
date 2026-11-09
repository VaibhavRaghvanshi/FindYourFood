using System.Text.Json.Serialization;
using FindYourFood.Hosting;
using FindYourFood.Organization.Api.HealthChecks;
using FindYourFood.Organization.Api.Middleware;
using FindYourFood.Organization.Domain;
using FindYourFood.Organization.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);
builder.AddFindYourFoodHosting("FindYourFood.Organization");
var connection = builder.Configuration.GetConnectionString("Organization")
    ?? throw new InvalidOperationException("Configure ConnectionStrings:Organization.");
builder.Services.AddOrganizationPersistence(connection);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHealthChecks().AddCheck<OrganizationReadiness>("organization-sql", tags: ["ready"]);
builder.Services.AddOpenApi();
builder.Services.AddControllers(options => options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true)
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(
        new JsonStringEnumConverter<StaffLevel>(allowIntegerValues: false)))
    .ConfigureApiBehaviorOptions(options => options.SuppressMapClientErrors = true);
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter<StaffLevel>(allowIntegerValues: false)));
TokenSettings.FromConfiguration(builder.Configuration).ConfigureBearer(builder.Services);

var app = builder.Build();
app.UseFindYourFoodHosting();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<GrantContentionMiddleware>();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapControllers();
}

app.Run();

public sealed class OrganizationHost;
