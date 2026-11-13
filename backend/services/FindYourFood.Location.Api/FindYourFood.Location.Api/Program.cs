using FindYourFood.Location.Api.HealthChecks;
using FindYourFood.Location.Infrastructure;
using FindYourFood.Hosting;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);
builder.AddFindYourFoodHosting("FindYourFood.Location");
builder.Services.AddControllers(options => options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true)
    .AddJsonOptions(options => options.JsonSerializerOptions.RespectRequiredConstructorParameters = true);
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks().AddCheck<LocationReadiness>("location-sql", tags: ["ready"]);
builder.Services.AddLocation(builder.Configuration.GetConnectionString("Location")
    ?? throw new InvalidOperationException("Configure ConnectionStrings:Location."));
builder.Services.AddAuthorizationBuilder().AddPolicy("zone-owner", policy => policy
    .RequireAuthenticatedUser().RequireClaim("account_type", "owner").RequireClaim("amr", "mfa"));
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

public sealed class LocationHost;
