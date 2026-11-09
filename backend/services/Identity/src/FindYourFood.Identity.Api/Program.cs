using FindYourFood.Hosting;
using FindYourFood.Identity.Api;
using FindYourFood.Identity.Api.Endpoints;
using FindYourFood.Identity.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);
builder.AddFindYourFoodHosting("FindYourFood.Identity");
builder.Services.AddOpenApi();
var connection = builder.Configuration.GetConnectionString("Identity")
    ?? throw new InvalidOperationException("Configure ConnectionStrings:Identity.");
builder.Services.AddIdentityPersistence(connection);
builder.Services.AddHealthChecks().AddCheck<IdentityReadiness>("identity-sql", tags: ["ready"]);
var tokens = TokenSettings.FromConfiguration(builder.Configuration);
tokens.ConfigureBearer(builder.Services);

if (builder.Environment.IsDevelopment())
{
    var privateKey = builder.Configuration["Tokens:PrivateKeyPath"]
        ?? throw new InvalidOperationException("Configure Tokens:PrivateKeyPath for local credential sign-in.");
    builder.Services.AddSingleton(TimeProvider.System);
    builder.Services.AddSingleton(services =>
        new DevelopmentTokenIssuer(tokens, privateKey, services.GetRequiredService<TimeProvider>()));
}

var app = builder.Build();
app.UseFindYourFoodHosting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });
if (app.Environment.IsDevelopment())
{
    _ = app.Services.GetRequiredService<DevelopmentTokenIssuer>();
    app.MapOpenApi();
    app.MapIdentityEndpoints();
}

app.Run();

public partial class Program;
