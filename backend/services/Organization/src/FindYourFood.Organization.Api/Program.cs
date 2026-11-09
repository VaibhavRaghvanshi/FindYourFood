using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json.Serialization;
using FindYourFood.Hosting;
using FindYourFood.Organization.Application;
using FindYourFood.Organization.Domain;
using FindYourFood.Organization.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);
builder.AddFindYourFoodHosting("FindYourFood.Organization");
var connection = OrganizationDbContext.OwnedConnection(builder.Configuration.GetConnectionString("Organization")
    ?? throw new InvalidOperationException("Configure ConnectionStrings:Organization."));
builder.Services.AddDbContext<OrganizationDbContext>(options => options.UseSqlServer(connection));
builder.Services.AddScoped<IGrantStore, SqlGrantStore>();
builder.Services.AddScoped<GrantAdministration>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHealthChecks().AddCheck<OrganizationReadiness>("organization-sql", tags: ["ready"]);
builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter<StaffLevel>(allowIntegerValues: false)));
TokenSettings.FromConfiguration(builder.Configuration).ConfigureBearer(builder.Services);

var app = builder.Build();
app.UseFindYourFoodHosting();
app.UseAuthentication();
app.UseAuthorization();
app.Use(async (http, next) =>
{
    http.Response.Headers.CacheControl = "no-store";
    try
    {
        await next(http);
    }
    catch (GrantContentionException)
    {
        await Results.Problem(statusCode: 503, title: "Permission records are busy. Retry the request.").ExecuteAsync(http);
    }
});
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    var access = app.MapGroup("/api/v1/organization").RequireAuthorization("staff").WithTags("Scoped staff access");
    access.MapGet("/grants", async (int? page, HttpContext http, GrantAdministration grants, CancellationToken ct) =>
    {
        if (GetActor(http) is not { } actor) return Results.Unauthorized();
        if (page is < 0 or > 1000) return Results.BadRequest();
        return Results.Ok(await grants.ListAsync(actor, page ?? 0, ct));
    });
    access.MapPost("/grants", async (GrantRequest input, HttpContext http, GrantAdministration grants, CancellationToken ct) =>
    {
        if (GetActor(http) is not { } actor) return Results.Unauthorized();
        var result = await grants.GrantAsync(actor, input, TraceId(http), ct);
        return result.Status == GrantStatus.Accepted
            ? Results.Json(result.Grant, statusCode: 201)
            : GrantError(result.Status);
    });
    access.MapDelete("/grants/{id:guid}", async (Guid id, HttpContext http, GrantAdministration grants, CancellationToken ct) =>
    {
        if (GetActor(http) is not { } actor) return Results.Unauthorized();
        var result = await grants.RevokeAsync(actor, id, TraceId(http), ct);
        return result.Status == GrantStatus.Accepted ? Results.NoContent() : GrantError(result.Status);
    });
    access.MapGet("/permissions/check", async (
        string permission, string scope, HttpContext http, GrantAdministration grants, CancellationToken ct) =>
    {
        if (GetActor(http) is not { } actor) return Results.Unauthorized();
        if (!StaffAccess.IsKnownPermission(permission) || !StaffAccess.IsScope(scope)) return Results.BadRequest();
        return Results.Ok(new { allowed = await grants.CheckAsync(actor, permission, scope, ct) });
    });
}

app.Run();

static StaffActor? GetActor(HttpContext http) =>
    Guid.TryParse(http.User.FindFirstValue("sub"), out var id) && id != Guid.Empty
        ? new StaffActor(id, http.User.HasClaim("account_type", "owner"))
        : null;

static string TraceId(HttpContext http) => Activity.Current?.TraceId.ToString() ?? http.TraceIdentifier;

static IResult GrantError(GrantStatus status) => status switch
{
    GrantStatus.Invalid => Results.Problem(statusCode: 400, title: "Check the permission, scope and staff level."),
    GrantStatus.Conflict => Results.Problem(statusCode: 409, title: "An unrevoked grant already exists for this subject, permission and scope."),
    GrantStatus.Missing => Results.NotFound(),
    _ => Results.Problem(statusCode: 403, title: "This account cannot make the requested access change.")
};

internal sealed class OrganizationReadiness(OrganizationDbContext database) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        await database.ProbeGrantsAsync(cancellationToken);
        await database.ProbeAuditAsync(cancellationToken);
        return HealthCheckResult.Healthy();
    }
}

public sealed class OrganizationHost;
