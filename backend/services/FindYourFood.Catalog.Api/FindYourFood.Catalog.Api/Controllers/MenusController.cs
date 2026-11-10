using FindYourFood.Catalog.Api.Authentication;
using FindYourFood.Catalog.Application.DTOs;
using FindYourFood.Catalog.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FindYourFood.Catalog.Api.Controllers;

[ApiController]
[Route("api/v1/catalog/branches/{branch:guid}")]
public sealed class MenusController(MenuPublishing publishing) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<PublishedMenu>> Public(Guid branch, CancellationToken ct) =>
        await publishing.PublishedAsync(branch, ct) is { } menu ? Ok(menu) : NotFound();

    [HttpGet("workspace")]
    [Authorize]
    [ServiceFilter(typeof(BranchOwnerFilter))]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<ActionResult<MenuWorkspace>> Workspace(Guid branch, CancellationToken ct) =>
        Ok(await publishing.WorkspaceAsync(branch, ct));

    [HttpPut("draft")]
    [Authorize]
    [ServiceFilter(typeof(BranchOwnerFilter))]
    [RequestSizeLimit(131_072)]
    public async Task<ActionResult<MenuWorkspace>> Save(Guid branch, MenuDraft request, CancellationToken ct) =>
        Outcome(await publishing.SaveDraftAsync(branch, request, ct));

    [HttpPost("publish")]
    [Authorize]
    [ServiceFilter(typeof(BranchOwnerFilter))]
    public async Task<ActionResult<MenuWorkspace>> Publish(Guid branch, PublishMenu request, CancellationToken ct) =>
        Outcome(await publishing.PublishAsync(branch, request, ct));

    private ActionResult Outcome(MenuResult result) => result.Status switch
    {
        MenuStatus.Accepted => Ok(result.Workspace),
        MenuStatus.Invalid => Problem(statusCode: 400, title: "Check item names, prices, labels, variants and add-ons."),
        _ => Problem(statusCode: 409, title: "The menu changed or has no unpublished draft. Reload the workspace.")
    };
}
