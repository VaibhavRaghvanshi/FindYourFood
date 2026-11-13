using System.ComponentModel.DataAnnotations;
using FindYourFood.Location.Application.DTOs;
using FindYourFood.Location.Application.Services;
using FindYourFood.Location.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FindYourFood.Location.Api.Controllers;

[ApiController]
[Route("api/v1/location")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class LocationController(DeliveryZones zones) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("serviceability")]
    [RequestSizeLimit(2048)]
    public async Task<ActionResult<Serviceability>> Check(Coordinates coordinates, CancellationToken ct) =>
        coordinates.IsValid ? Ok(await zones.CheckAsync(coordinates, ct))
            : Problem(statusCode: 400, title: "Enter a valid latitude and longitude.");

    [Authorize(Policy = "zone-owner")]
    [HttpGet("zones")]
    public Task<ZoneSnapshot[]> Page([Range(1, 10_000)] int page = 1, CancellationToken ct = default) =>
        zones.PageAsync(page, ct);

    [AllowAnonymous]
    [HttpPost("route-quote")]
    [RequestSizeLimit(4096)]
    public async Task<ActionResult<Serviceability>> Quote(DeliveryQuoteRequest input, CancellationToken ct) =>
        input.Origin?.IsValid == true && input.Destination?.IsValid == true
            ? Ok(await zones.QuoteAsync(input, ct))
            : Problem(statusCode: 400, title: "Enter valid origin and destination coordinates.");

    [Authorize(Policy = "zone-owner")]
    [HttpPut("zones/{id:guid}")]
    [RequestSizeLimit(8192)]
    public async Task<ActionResult<ZoneSnapshot>> Save(Guid id, ZoneUpdate input, CancellationToken ct)
    {
        var result = await zones.SaveAsync(id, input, ct);
        return result.Status switch
        {
            ZoneSaveStatus.Saved => Ok(result.Zone),
            ZoneSaveStatus.Invalid => Problem(statusCode: 400, title: "Check the zone name, coordinates, radius and fee."),
            _ => Problem(statusCode: 409, title: "The zone changed. Reload before saving again.")
        };
    }

    [AllowAnonymous]
    [HttpPost("coverage-matrix")]
    [RequestSizeLimit(131_072)]
    public async Task<ActionResult<Serviceability[]>> Matrix(CoverageMatrixRequest input, CancellationToken ct) =>
        input.Destination?.IsValid == true && input.Origins is { Count: > 0 and <= 1000 } &&
            input.Origins.All(point => point?.IsValid == true)
            ? Ok(await zones.MatrixAsync(input, ct))
            : Problem(statusCode: 400, title: "Enter a destination and up to 1000 valid origins.");
}
