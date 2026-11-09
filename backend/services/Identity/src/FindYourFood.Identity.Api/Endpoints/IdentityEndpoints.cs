using System.Security.Claims;
using FindYourFood.Identity.Application;

namespace FindYourFood.Identity.Api.Endpoints;

internal static class IdentityEndpoints
{
    public static void MapIdentityEndpoints(this WebApplication app)
    {
        var identity = app.MapGroup("/api/v1/identity").WithTags("Customer identity");
        identity.MapPost("/register", RegisterAsync)
            .AllowAnonymous()
            .RequireRateLimiting("identity-write")
            .Produces(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);
        identity.MapPost("/login", LoginAsync)
            .AllowAnonymous()
            .RequireRateLimiting("identity-write")
            .Produces<AccessTokenResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        identity.MapGet("/me", ProfileAsync)
            .RequireAuthorization()
            .Produces<CustomerProfile>()
            .Produces(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> RegisterAsync(Credentials input, CustomerAccounts accounts)
    {
        var result = await accounts.RegisterAsync(input.Email, input.Password);
        return result switch
        {
            RegistrationResult.Created => Results.StatusCode(StatusCodes.Status201Created),
            RegistrationResult.AlreadyRegistered => Results.Problem(
                statusCode: StatusCodes.Status409Conflict, title: "Unable to create this account."),
            _ => Results.Problem(statusCode: StatusCodes.Status400BadRequest,
                title: "Enter a valid email and a password between 12 and 128 characters.")
        };
    }

    private static async Task<IResult> LoginAsync(
        Credentials input, CustomerAccounts accounts, DevelopmentTokenIssuer issuer, HttpContext http)
    {
        http.Response.Headers.CacheControl = "no-store";
        var account = await accounts.AuthenticateAsync(input.Email, input.Password);
        return account is null
            ? Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Sign-in failed.")
            : Results.Ok(issuer.Issue(account.Id));
    }

    private static async Task<IResult> ProfileAsync(ClaimsPrincipal principal, CustomerAccounts accounts, HttpContext http)
    {
        http.Response.Headers.CacheControl = "no-store";
        if (!Guid.TryParse(principal.FindFirstValue("sub"), out var id))
        {
            return Results.Unauthorized();
        }

        var account = await accounts.FindAsync(id);
        return account is null
            ? Results.Unauthorized()
            : Results.Ok(new CustomerProfile(account.Id, account.Email, account.EmailVerified));
    }

    private sealed record Credentials(string? Email, string? Password);
    private sealed record CustomerProfile(Guid Id, string Email, bool EmailVerified);
}
