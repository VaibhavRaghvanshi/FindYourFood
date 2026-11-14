using System.Text.Json;
using FindYourFood.Hosting;
using FindYourFood.Payment.Application.DTOs;
using FindYourFood.Payment.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace FindYourFood.Payment.Api.Controllers;

[ApiController]
[Route("api/v1/payments/orders/{order:guid}")]
[ServiceFilter(typeof(CustomerRequestFilter))]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PaymentsController(Payments payments, IWebHostEnvironment environment) : ControllerBase
{
    private PaymentActor Actor => new(Guid.TryParse(User.FindFirst("sub")?.Value, out var subject) ? subject : null,
        Request.Headers["X-Cart-Key"].FirstOrDefault(), Request.Headers.Authorization.FirstOrDefault());
    [HttpGet]
    public async Task<ActionResult> Get(Guid order, CancellationToken ct) => Outcome(await payments.GetAsync(Actor, order, ct));
    [HttpPost("simulate")]
    [RequestSizeLimit(2048)]
    public async Task<ActionResult> Simulate(Guid order, SimulatePayment input, CancellationToken ct)
    {
        if (!environment.IsDevelopment()) return NotFound();
        try { return Outcome(await payments.SimulateAsync(Actor, order, input, ct)); }
        catch (Exception error) when (error is HttpRequestException or JsonException ||
            (error is OperationCanceledException && !ct.IsCancellationRequested))
        { return Problem(statusCode: 503, title: "Payment confirmation is pending. Retry with the same attempt."); }
    }
    private ActionResult Outcome(PaymentResult result) => result.Status switch
    {
        PaymentResultStatus.Accepted => Ok(result.Payment),
        PaymentResultStatus.Processing => Accepted(value: result.Payment),
        PaymentResultStatus.Missing => NotFound(),
        PaymentResultStatus.Invalid => Problem(statusCode: 400, title: "Check the payment amount and attempt."),
        _ => Problem(statusCode: 409, title: "This payment changed. Refresh the order before retrying.")
    };
}
