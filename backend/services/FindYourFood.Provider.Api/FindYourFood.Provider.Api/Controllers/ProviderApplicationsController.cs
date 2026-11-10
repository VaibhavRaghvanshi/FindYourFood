using FindYourFood.Provider.Application.DTOs;
using FindYourFood.Provider.Application.Services;
using FindYourFood.Provider.Domain;
using Microsoft.AspNetCore.Mvc;

namespace FindYourFood.Provider.Api.Controllers;

[Route("api/v1/providers/me")]
public sealed class ProviderApplicationsController(ProviderOnboarding onboarding) : ProviderControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ProviderView>> Get(CancellationToken ct) =>
        await onboarding.OwnedAsync(SubjectId, ct) is { } profile ? Ok(profile) : NotFound();

    [HttpPost]
    public async Task<ActionResult<ProviderView>> Create(ProviderDetails details, CancellationToken ct) =>
        ProviderResponse(await onboarding.CreateAsync(SubjectId, details, ct), "Check the provider application fields.", created: true);

    [HttpPut]
    public async Task<ActionResult<ProviderView>> Update(UpdateProviderRequest request, CancellationToken ct) =>
        ProviderResponse(await onboarding.UpdateAsync(SubjectId, request, ct), "Check the provider application fields.");

    [HttpPut("documents/{kind}")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(2_100_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = ProviderRules.MaxDocumentBytes)]
    public async Task<ActionResult<ProviderView>> Upload(
        string kind, [FromForm] int expectedRevision, IFormFile? document, CancellationToken ct)
    {
        const string invalid = "Upload a PDF, PNG or JPEG up to 2 MB for a supported document kind.";
        if (!ProviderRules.ValidDocumentKind(kind) || document is null ||
            document.Length is <= 0 or > ProviderRules.MaxDocumentBytes)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: invalid);
        }

        using var content = new MemoryStream((int)document.Length);
        await document.CopyToAsync(content, ct);
        return ProviderResponse(await onboarding.UploadAsync(SubjectId, kind, expectedRevision, content.ToArray(), ct), invalid);
    }

    [HttpGet("documents/{kind}")]
    public async Task<IActionResult> Download(string kind, CancellationToken ct) =>
        Attachment(await onboarding.OwnedDocumentAsync(SubjectId, kind, ct));

    [HttpPost("submit")]
    public async Task<ActionResult<ProviderView>> Submit(SubmitRequest request, CancellationToken ct) =>
        ProviderResponse(await onboarding.SubmitAsync(SubjectId, request.ExpectedRevision, ct),
            "Add the business license and address proof before submitting.");

    [HttpPost("branches")]
    public async Task<ActionResult<ProviderView>> CreateBranch(UpdateBranchRequest request, CancellationToken ct) =>
        ProviderResponse(await onboarding.SaveBranchAsync(SubjectId, null, request, ct),
            "Check the branch address, time zone, opening hours and closure.", created: true);

    [HttpPut("branches/{id:guid}")]
    public async Task<ActionResult<ProviderView>> UpdateBranch(Guid id, UpdateBranchRequest request, CancellationToken ct) =>
        ProviderResponse(await onboarding.SaveBranchAsync(SubjectId, id, request, ct),
            "Check the branch address, time zone, opening hours and closure.");

    [HttpGet("branches/{id:guid}/access")]
    public async Task<IActionResult> BranchAccess(Guid id, CancellationToken ct) =>
        await onboarding.CanManageBranchAsync(SubjectId, id, ct) ? NoContent() : NotFound();
}
