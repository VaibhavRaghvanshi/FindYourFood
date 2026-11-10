using FindYourFood.Experience.Application.DTOs;
using FindYourFood.Experience.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FindYourFood.Experience.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/experience/customer-home")]
public sealed class PublicExperienceController(ExperiencePublishing publishing) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PublishedExperience>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-cache";
        var published = await publishing.PublishedAsync(cancellationToken);
        return published is null ? NoContent() : Ok(published);
    }
}
