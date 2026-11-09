using FindYourFood.Identity.Api.Authentication;
using FindYourFood.Identity.Application.DTOs;
using FindYourFood.Identity.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FindYourFood.Identity.Api.Controllers;

[ApiController]
[Route("api/v1/identity/staff")]
[Tags("Staff identity")]
public sealed class StaffAuthenticationController(PrivilegedAccounts accounts, DevelopmentTokenIssuer issuer) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("identity-write")]
    [ProducesResponseType<AccessTokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(StaffCredentials input)
    {
        Response.Headers.CacheControl = "no-store";
        var account = await accounts.AuthenticateAsync(input.Email, input.Password, input.Code);
        return account is null
            ? Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Sign-in failed.")
            : Ok(issuer.IssuePrivileged(account));
    }
}
