using System.Security.Claims;
using System.Text.Json.Serialization;
using FindYourFood.Hosting;
using FindYourFood.Provider.Api.Authentication;
using FindYourFood.Provider.Api.HealthChecks;
using FindYourFood.Provider.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);
builder.AddFindYourFoodHosting("FindYourFood.Provider");
builder.Services.AddControllers(options => options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true)
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
        options.JsonSerializerOptions.RespectRequiredConstructorParameters = true;
    });
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks().AddCheck<ProviderReadiness>("provider-sql", tags: ["ready"]);
builder.Services.AddProvider(builder.Configuration.GetConnectionString("Provider")
    ?? throw new InvalidOperationException("Configure ConnectionStrings:Provider."));
builder.Services.AddOrganizationPermissionClient(new Uri(builder.Configuration["Organization:BaseUrl"]
    ?? throw new InvalidOperationException("Configure Organization:BaseUrl."), UriKind.Absolute));
builder.Services.AddScoped<ProviderReviewFilter>();
TokenSettings.FromConfiguration(builder.Configuration).ConfigureBearer(builder.Services);
builder.Services.AddAuthorization(options => options.AddPolicy("provider-subject", policy => policy
    .RequireAuthenticatedUser().RequireAssertion(context =>
        Guid.TryParse(context.User.FindFirstValue("sub"), out var subject) && subject != Guid.Empty)));

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

public sealed class ProviderHost;
