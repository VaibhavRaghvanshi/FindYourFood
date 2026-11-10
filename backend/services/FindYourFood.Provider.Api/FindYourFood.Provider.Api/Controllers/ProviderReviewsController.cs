using FindYourFood.Provider.Api.Authentication;
using FindYourFood.Provider.Application.DTOs;
using FindYourFood.Provider.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FindYourFood.Provider.Api.Controllers;

[Authorize(Policy = "staff")]
[ServiceFilter(typeof(ProviderReviewFilter))]
[Route("api/v1/providers/admin")]
public sealed class ProviderReviewsController(ProviderOnboarding onboarding) : ProviderControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProviderSummary>>> Pending(CancellationToken ct) =>
        Ok(await onboarding.PendingAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProviderView>> Get(Guid id, CancellationToken ct) =>
        await onboarding.FindAsync(id, ct) is { } profile ? Ok(profile) : NotFound();

    [HttpGet("{id:guid}/documents/{kind}")]
    public async Task<IActionResult> Download(Guid id, string kind, CancellationToken ct) =>
        Attachment(await onboarding.DocumentAsync(id, kind, ct));

    [HttpPost("{id:guid}/review")]
    public async Task<ActionResult<ProviderView>> Review(Guid id, ReviewRequest request, CancellationToken ct) =>
        ProviderResponse(await onboarding.ReviewAsync(id, SubjectId, request, ct),
            "Enter a review note of up to 300 characters. A note is required when rejecting an application.");
}
