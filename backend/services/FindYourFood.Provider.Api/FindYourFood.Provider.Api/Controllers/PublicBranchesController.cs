using System.ComponentModel.DataAnnotations;
using FindYourFood.Provider.Application.Interfaces;
using FindYourFood.Provider.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FindYourFood.Provider.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/providers/public/branches")]
public sealed class PublicBranchesController(IProviderRepository providers) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<PublicBranchView>> Directory(
        [Required, StringLength(80, MinimumLength = 1)] string city,
        [Range(1, 200)] int page = 1, CancellationToken ct = default) =>
        providers.PublicAsync(city.Trim(), null, page, ct);

    [HttpGet("{branch:guid}")]
    public async Task<ActionResult<PublicBranchView>> Branch(Guid branch, CancellationToken ct) =>
        (await providers.PublicAsync(null, branch, 1, ct)).SingleOrDefault() is { } item ? Ok(item) : NotFound();
}
