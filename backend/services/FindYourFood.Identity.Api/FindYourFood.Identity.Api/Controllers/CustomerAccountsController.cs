using System.Security.Claims;
using FindYourFood.Identity.Api.Authentication;
using FindYourFood.Identity.Application.DTOs;
using FindYourFood.Identity.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FindYourFood.Identity.Api.Controllers;

[ApiController]
[Route("api/v1/identity")]
[Tags("Customer identity")]
public sealed class CustomerAccountsController(CustomerAccounts accounts, DevelopmentTokenIssuer issuer) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("identity-write")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(Credentials input)
    {
        var result = await accounts.RegisterAsync(input.Email, input.Password);
        return result switch
        {
            RegistrationResult.Created => StatusCode(StatusCodes.Status201Created),
            RegistrationResult.AlreadyRegistered => Problem(
                statusCode: StatusCodes.Status409Conflict, title: "Unable to create this account."),
            _ => Problem(statusCode: StatusCodes.Status400BadRequest,
                title: "Enter a valid email and a password between 12 and 128 characters.")
        };
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("identity-write")]
    [ProducesResponseType<AccessTokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(Credentials input)
    {
        Response.Headers.CacheControl = "no-store";
        var account = await accounts.AuthenticateAsync(input.Email, input.Password);
        return account is null
            ? Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Sign-in failed.")
            : Ok(issuer.Issue(account.Id));
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType<CustomerProfile>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Profile()
    {
        Response.Headers.CacheControl = "no-store";
        if (!Guid.TryParse(User.FindFirstValue("sub"), out var id))
        {
            return Unauthorized();
        }

        var account = await accounts.FindAsync(id);
        return account is null
            ? Unauthorized()
            : Ok(new CustomerProfile(account.Id, account.Email, account.EmailVerified));
    }
}
