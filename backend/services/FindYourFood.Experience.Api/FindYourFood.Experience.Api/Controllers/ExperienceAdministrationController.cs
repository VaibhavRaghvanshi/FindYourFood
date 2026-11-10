using System.Security.Claims;
using FindYourFood.Experience.Api.Authentication;
using FindYourFood.Experience.Application.DTOs;
using FindYourFood.Experience.Application.Interfaces;
using FindYourFood.Experience.Application.Services;
using FindYourFood.Experience.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FindYourFood.Experience.Api.Controllers;

[ApiController]
[Authorize(Policy = "staff")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("api/v1/experience/admin/customer-home")]
public sealed class ExperienceAdministrationController(ExperiencePublishing publishing) : ControllerBase
{
    [HttpGet]
    [ExperiencePermission(ExperiencePermissions.Edit, ExperiencePermissions.Publish)]
    public async Task<ActionResult<WorkspaceView>> Workspace(CancellationToken cancellationToken) =>
        Ok(await publishing.WorkspaceAsync(cancellationToken));

    [HttpPost("preview")]
    [ExperiencePermission(ExperiencePermissions.Edit, ExperiencePermissions.Publish)]
    public ActionResult<ExperienceDocument> Preview(PreviewRequest request) =>
        ExperienceRules.IsValid(request.Configuration) ? Ok(request.Configuration) : InvalidConfiguration();

    [HttpPut("draft")]
    [ExperiencePermission(ExperiencePermissions.Edit)]
    public async Task<ActionResult<WorkspaceView>> SaveDraft(SaveDraftRequest request, CancellationToken cancellationToken) =>
        Result(await publishing.SaveDraftAsync(request, cancellationToken));

    [HttpPost("approve")]
    [ExperiencePermission(ExperiencePermissions.Publish)]
    public async Task<ActionResult<WorkspaceView>> Approve(RevisionRequest request, CancellationToken cancellationToken) =>
        Result(await publishing.ApproveAsync(request.ExpectedRevision, ActorId, cancellationToken));

    [HttpPost("publish")]
    [ExperiencePermission(ExperiencePermissions.Publish)]
    public async Task<ActionResult<WorkspaceView>> Publish(RevisionRequest request, CancellationToken cancellationToken) =>
        Result(await publishing.PublishAsync(request.ExpectedRevision, ActorId, cancellationToken));

    [HttpPost("rollback")]
    [ExperiencePermission(ExperiencePermissions.Publish)]
    public async Task<ActionResult<WorkspaceView>> Rollback(RollbackRequest request, CancellationToken cancellationToken) =>
        Result(await publishing.RollbackAsync(request, ActorId, cancellationToken));

    private Guid ActorId => Guid.Parse(User.FindFirstValue("sub")!);

    private ActionResult Result(WorkflowResult result) => result.Status switch
    {
        WorkflowStatus.Accepted => Ok(result.Workspace),
        WorkflowStatus.Invalid => InvalidConfiguration(),
        WorkflowStatus.Missing => NotFound(),
        _ => Problem(statusCode: StatusCodes.Status409Conflict,
            title: "The workspace changed or this transition is unavailable. Reload the workspace.")
    };

    private ObjectResult InvalidConfiguration() => Problem(statusCode: StatusCodes.Status400BadRequest,
        title: "Use schema version 1 and supported theme, navigation and section values.");
}
