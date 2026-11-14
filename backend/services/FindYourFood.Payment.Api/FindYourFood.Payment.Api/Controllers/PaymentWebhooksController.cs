using System.Text.Json;
using FindYourFood.Contracts;
using FindYourFood.Payment.Api.Workers;
using FindYourFood.Payment.Application.DTOs;
using FindYourFood.Payment.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace FindYourFood.Payment.Api.Controllers;

[ApiController]
[Route("api/v1/payments/webhooks/development")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PaymentWebhooksController(Payments payments, GatewaySignature signature,
    IWebHostEnvironment environment) : ControllerBase
{
    [HttpPost]
    [RequestSizeLimit(8192)]
    public async Task<ActionResult> Handle(CancellationToken ct)
    {
        if (!environment.IsDevelopment()) return NotFound();
        using var buffer = new MemoryStream();
        await Request.Body.CopyToAsync(buffer, ct);
        var body = buffer.ToArray();
        if (!signature.Verify(Request.Headers["X-Payment-Timestamp"].ToString(),
            Request.Headers["X-Payment-Signature"].ToString(), body)) return Unauthorized();
        try
        {
            var completion = JsonSerializer.Deserialize<GatewayCompletion>(body, IntegrationEvent.Json);
            if (completion is null) return BadRequest();
            await payments.HandleGatewayAsync(completion, ct);
            return Ok();
        }
        catch (Exception error) when (error is JsonException or InvalidIntegrationEventException)
        { return BadRequest(new { title = "Invalid payment event." }); }
    }
}
