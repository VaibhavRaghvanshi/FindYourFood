using System.Security.Claims;
using FindYourFood.Provider.Application.DTOs;
using FindYourFood.Provider.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FindYourFood.Provider.Api.Controllers;

[ApiController]
[Authorize(Policy = "provider-subject")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public abstract class ProviderControllerBase : ControllerBase
{
    protected Guid SubjectId => Guid.Parse(User.FindFirstValue("sub")!);

    protected ActionResult ProviderResponse(ProviderResult result, string invalidMessage, bool created = false) =>
        result.Status switch
        {
            ProviderResultStatus.Accepted when created => StatusCode(StatusCodes.Status201Created, result.Provider),
            ProviderResultStatus.Accepted => Ok(result.Provider),
            ProviderResultStatus.Missing => NotFound(),
            ProviderResultStatus.Invalid => Problem(statusCode: StatusCodes.Status400BadRequest, title: invalidMessage),
            _ => Problem(statusCode: StatusCodes.Status409Conflict,
                title: "The provider profile changed or this transition is unavailable. Reload the profile.")
        };

    protected IActionResult Attachment(VerificationDocument? document)
    {
        if (document is null) return NotFound();
        Response.Headers.XContentTypeOptions = "nosniff";
        var extension = document.MediaType switch
        {
            "application/pdf" => "pdf",
            "image/png" => "png",
            _ => "jpg"
        };
        return File(document.Content, document.MediaType, $"{document.Kind}.{extension}");
    }
}
