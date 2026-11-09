using System.Diagnostics;
using FindYourFood.Organization.Api.Authentication;
using FindYourFood.Organization.Application.DTOs;
using FindYourFood.Organization.Application.Services;
using FindYourFood.Organization.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FindYourFood.Organization.Api.Controllers;

[ApiController]
[Authorize(Policy = "staff")]
[Route("api/v1/organization/grants")]
[Tags("Scoped staff access")]
public sealed class GrantsController(GrantAdministration grants) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AccessGrant>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] int? page, CancellationToken cancellationToken)
    {
        if (User.GetStaffActor() is not { } actor)
        {
            return Unauthorized();
        }

        if (page is < 0 or > 1000)
        {
            return BadRequest();
        }

        return Ok(await grants.ListAsync(actor, page ?? 0, cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType<AccessGrant>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Grant(GrantRequest input, CancellationToken cancellationToken)
    {
        if (User.GetStaffActor() is not { } actor)
        {
            return Unauthorized();
        }

        var result = await grants.GrantAsync(actor, input, TraceId(), cancellationToken);
        return result.Status == GrantStatus.Accepted
            ? StatusCode(StatusCodes.Status201Created, result.Grant)
            : GrantError(result.Status);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Revoke(Guid id, CancellationToken cancellationToken)
    {
        if (User.GetStaffActor() is not { } actor)
        {
            return Unauthorized();
        }

        var result = await grants.RevokeAsync(actor, id, TraceId(), cancellationToken);
        return result.Status == GrantStatus.Accepted ? NoContent() : GrantError(result.Status);
    }

    private string TraceId() => Activity.Current?.TraceId.ToString() ?? HttpContext.TraceIdentifier;

    private IActionResult GrantError(GrantStatus status) => status switch
    {
        GrantStatus.Invalid => Problem(statusCode: 400, title: "Check the permission, scope and staff level."),
        GrantStatus.Conflict => Problem(statusCode: 409, title: "An unrevoked grant already exists for this subject, permission and scope."),
        GrantStatus.Missing => NotFound(),
        _ => Problem(statusCode: 403, title: "This account cannot make the requested access change.")
    };
}
