using FindYourFood.Organization.Api.Authentication;
using FindYourFood.Organization.Application.DTOs;
using FindYourFood.Organization.Application.Services;
using FindYourFood.Organization.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FindYourFood.Organization.Api.Controllers;

[ApiController]
[Authorize(Policy = "staff")]
[Route("api/v1/organization/permissions")]
[Tags("Scoped staff access")]
public sealed class PermissionsController(GrantAdministration grants) : ControllerBase
{
    [HttpGet("check")]
    [ProducesResponseType<PermissionDecision>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Check(
        [FromQuery] string permission, [FromQuery] string scope, CancellationToken cancellationToken)
    {
        if (User.GetStaffActor() is not { } actor)
        {
            return Unauthorized();
        }

        if (!StaffAccess.IsKnownPermission(permission) || !StaffAccess.IsScope(scope))
        {
            return BadRequest();
        }

        return Ok(new PermissionDecision(await grants.CheckAsync(actor, permission, scope, cancellationToken)));
    }
}
