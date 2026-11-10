using FindYourFood.Catalog.Api.Authentication;
using FindYourFood.Catalog.Api.HealthChecks;
using FindYourFood.Catalog.Infrastructure;
using FindYourFood.Hosting;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);
builder.AddFindYourFoodHosting("FindYourFood.Catalog");
builder.Services.AddControllers(options => options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true)
    .AddJsonOptions(options => options.JsonSerializerOptions.RespectRequiredConstructorParameters = true);
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks().AddCheck<CatalogReadiness>("catalog-sql", tags: ["ready"]);
builder.Services.AddCatalog(builder.Configuration.GetConnectionString("Catalog")
    ?? throw new InvalidOperationException("Configure ConnectionStrings:Catalog."));
builder.Services.AddHttpClient("provider", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Provider:BaseUrl"]!, UriKind.Absolute);
    client.Timeout = TimeSpan.FromSeconds(3);
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
builder.Services.AddScoped<BranchOwnerFilter>();
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

public sealed class CatalogHost;
