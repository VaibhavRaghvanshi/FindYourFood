using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using FindYourFood.Hosting;
using FindYourFood.Ordering.Application.DTOs;
using FindYourFood.Ordering.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace FindYourFood.Ordering.Api.Controllers;

[ApiController]
[Route("api/v1/orders")]
[ServiceFilter(typeof(CustomerRequestFilter))]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class OrdersController(OrderCheckout checkout) : ControllerBase
{
    private OrderActor Actor => new(Guid.TryParse(User.FindFirst("sub")?.Value, out var subject) ? subject : null,
        Request.Headers["X-Cart-Key"].FirstOrDefault(), Request.Headers.Authorization.FirstOrDefault());

    [HttpPost]
    [RequestSizeLimit(8192)]
    public Task<ActionResult> Create(CheckoutRequest input, CancellationToken ct) => Execute(async () =>
        Outcome(await checkout.CreateAsync(Actor, input, ct)), ct);

    [HttpGet("{id:guid}")]
    public Task<ActionResult> Get(Guid id, CancellationToken ct) => Execute(async () =>
        Outcome(await checkout.GetAsync(Actor, id, ct)), ct);

    [HttpGet]
    public async Task<ActionResult<OrderSummary[]>> History([Range(1, 500)] int page = 1, CancellationToken ct = default) =>
        Ok(await checkout.HistoryAsync(Actor, page, ct));

    [HttpPost("{id:guid}/cancel")]
    [RequestSizeLimit(1024)]
    public Task<ActionResult> Cancel(Guid id, CancellationToken ct) => Execute(async () =>
        Outcome(await checkout.CancelAsync(Actor, id, ct)), ct);

    private ActionResult Outcome(OrderResult result) => result.Status switch
    {
        OrderResultStatus.Created => Created($"/api/v1/orders/{result.Order!.Id:D}", result.Order),
        OrderResultStatus.Accepted => Ok(result.Order),
        OrderResultStatus.Missing => NotFound(),
        OrderResultStatus.Invalid => Problem(statusCode: 400, title: "Check your cart, delivery address and contact number."),
        OrderResultStatus.PriceChanged => Problem(statusCode: 409, title: "Your cart or total changed. Get a new delivery quote and review it."),
        _ => Conflict(new { title = "This checkout already exists or changed.", existingOrderId = result.ExistingOrderId })
    };

    private async Task<ActionResult> Execute(Func<Task<ActionResult>> action, CancellationToken ct)
    {
        try { return await action(); }
        catch (Exception error) when (error is HttpRequestException or JsonException ||
            (error is OperationCanceledException && !ct.IsCancellationRequested))
        {
            return Problem(statusCode: 503, title: "Checkout is temporarily unavailable. You can retry this checkout.");
        }
    }
}
