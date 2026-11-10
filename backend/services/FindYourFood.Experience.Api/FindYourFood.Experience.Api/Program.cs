using System.Text.Json.Serialization;
using FindYourFood.Experience.Api.HealthChecks;
using FindYourFood.Experience.Infrastructure;
using FindYourFood.Hosting;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);
builder.AddFindYourFoodHosting("FindYourFood.Experience");
builder.Services.AddControllers(options => options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true)
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
        options.JsonSerializerOptions.RespectRequiredConstructorParameters = true;
    });
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks().AddCheck<ExperienceReadiness>("experience-sql", tags: ["ready"]);
var connection = builder.Configuration.GetConnectionString("Experience")
    ?? throw new InvalidOperationException("Configure ConnectionStrings:Experience.");
var organization = new Uri(builder.Configuration["Organization:BaseUrl"]
    ?? throw new InvalidOperationException("Configure Organization:BaseUrl."), UriKind.Absolute);
builder.Services.AddExperience(connection, organization);
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

public sealed class ExperienceHost;
