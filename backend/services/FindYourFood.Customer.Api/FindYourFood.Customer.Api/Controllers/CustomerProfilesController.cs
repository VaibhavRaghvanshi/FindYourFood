using FindYourFood.Customer.Application.DTOs;
using FindYourFood.Customer.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FindYourFood.Customer.Api.Controllers;

[ApiController]
[Route("api/v1/customer/me")]
[Authorize(Policy = "customer")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class CustomerProfilesController(CustomerProfiles profiles) : ControllerBase
{
    private Guid Subject => Guid.Parse(User.FindFirst("sub")!.Value);

    [HttpGet]
    public Task<CustomerSnapshot> Get(CancellationToken ct) => profiles.GetAsync(Subject, ct);

    [HttpPut]
    [RequestSizeLimit(32_768)]
    public async Task<ActionResult<CustomerSnapshot>> Save(CustomerSnapshot input, CancellationToken ct)
    {
        var result = await profiles.SaveAsync(Subject, input, ct);
        return result.Status switch
        {
            ProfileSaveStatus.Saved => Ok(result.Profile),
            ProfileSaveStatus.Invalid => Problem(statusCode: 400, title: "Check your profile settings."),
            _ => Problem(statusCode: 409, title: "Your settings changed. Reload before saving again.")
        };
    }
}
