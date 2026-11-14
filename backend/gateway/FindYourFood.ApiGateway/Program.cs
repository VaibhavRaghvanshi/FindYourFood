using FindYourFood.Hosting;

var builder = WebApplication.CreateBuilder(args);
if (builder.Environment.IsDevelopment() && builder.Configuration.GetValue<bool>("Gateway:EnableOrganization"))
{
    builder.Configuration.AddJsonFile("organization.routes.json", optional: false);
}
if (builder.Environment.IsDevelopment() && builder.Configuration.GetValue<bool>("Gateway:EnableExperience"))
{
    builder.Configuration.AddJsonFile("experience.routes.json", optional: false);
}
if (builder.Environment.IsDevelopment() && builder.Configuration.GetValue<bool>("Gateway:EnableProvider"))
{
    builder.Configuration.AddJsonFile("provider.routes.json", optional: false);
}
if (builder.Environment.IsDevelopment())
{
    foreach (var service in new[] { "Catalog", "Customer", "Location", "Discovery", "Cart", "Ordering", "Payment" })
    {
        if (builder.Configuration.GetValue<bool>($"Gateway:Enable{service}"))
        {
            builder.Configuration.AddJsonFile($"{service.ToLowerInvariant()}.routes.json", optional: false);
        }
    }
}
builder.AddFindYourFoodHosting("FindYourFood.ApiGateway");
if (builder.Environment.IsDevelopment() && builder.Configuration.GetValue<bool>("Gateway:ConstrainedWorkstation"))
{
    foreach (var cluster in builder.Configuration.GetSection("ReverseProxy:Clusters").GetChildren())
    {
        builder.Configuration[$"ReverseProxy:Clusters:{cluster.Key}:HealthCheck:Active:Interval"] = "00:00:30";
        builder.Configuration[$"ReverseProxy:Clusters:{cluster.Key}:HealthCheck:Active:Timeout"] = "00:00:10";
    }
}
TokenSettings.FromConfiguration(builder.Configuration).ConfigureBearer(builder.Services);
builder.Services.AddHealthChecks();
builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();
app.UseFindYourFoodHosting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health/live");
app.MapReverseProxy();
app.Run();
