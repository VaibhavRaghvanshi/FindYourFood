using System.Text.Json;
using FindYourFood.Cart.Application.DTOs;
using FindYourFood.Cart.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FindYourFood.Cart.Api.Controllers;

[ApiController]
[Route("api/v1/carts")]
[ServiceFilter(typeof(CartCustomerFilter))]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class CartsController(ShoppingCarts carts) : ControllerBase
{
    private CartActor Actor => new(
        Guid.TryParse(User.FindFirst("sub")?.Value, out var subject) ? subject : null,
        Request.Headers["X-Cart-Key"].FirstOrDefault());

    [HttpPost]
    [RequestSizeLimit(2048)]
    public Task<ActionResult> Create(CreateCart input, CancellationToken ct) => Execute(async () =>
    {
        var result = await carts.CreateAsync(Actor, input.BranchId, ct);
        return result.Status == CartStatus.Accepted
            ? Created($"/api/v1/carts/{result.Cart!.Id:D}", new CreatedCart(result.Cart, result.GuestKey)) : Outcome(result);
    }, ct);

    [HttpGet("mine")]
    [Authorize]
    public Task<ActionResult> Mine(CancellationToken ct) => Execute(async () =>
        Outcome(await carts.ReadMineAsync(Actor, ct)), ct);

    [HttpGet("{id:guid}")]
    public Task<ActionResult> Get(Guid id, CancellationToken ct) => Execute(async () =>
        Outcome(await carts.ReadAsync(Actor, id, ct)), ct);

    [HttpPut("{id:guid}")]
    [RequestSizeLimit(32_768)]
    public Task<ActionResult> Change(Guid id, ChangeCart input, CancellationToken ct) => Execute(async () =>
        Outcome(await carts.ChangeAsync(Actor, id, input, ct)), ct);

    [HttpDelete("{id:guid}")]
    public Task<ActionResult> Remove(Guid id, CancellationToken ct) => Execute(async () =>
    {
        var result = await carts.RemoveAsync(Actor, id, ct);
        return result.Status == CartStatus.Accepted ? NoContent() : Outcome(result);
    }, ct);

    [HttpPost("{id:guid}/quote")]
    [RequestSizeLimit(4096)]
    public Task<ActionResult> Quote(Guid id, QuoteRequest input, CancellationToken ct) => Execute(async () =>
        Outcome(await carts.QuoteAsync(Actor, id, input, ct)), ct);

    private ActionResult Outcome(CartResult result) => result.Status switch
    {
        CartStatus.Accepted => result.Quote is not null ? Ok(result.Quote) : Ok(result.Cart),
        CartStatus.Missing => NotFound(),
        CartStatus.Invalid => Problem(statusCode: 400, title: "Check your cart items and delivery location."),
        CartStatus.ReviewRequired => Problem(statusCode: 409, title: "Review availability, options, opening hours and delivery coverage."),
        _ => Problem(statusCode: 409, title: "Your cart changed. Reload before trying again.")
    };

    private async Task<ActionResult> Execute(Func<Task<ActionResult>> action, CancellationToken ct)
    {
        try { return await action(); }
        catch (Exception error) when (error is HttpRequestException or JsonException ||
            (error is OperationCanceledException && !ct.IsCancellationRequested))
        {
            return Problem(statusCode: 503, title: "Cart pricing is temporarily unavailable. Try again.");
        }
    }
}

public sealed class CartCustomerFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var user = context.HttpContext.User;
        if (user.HasClaim(claim => claim.Type == "account_type") ||
            (user.Identity?.IsAuthenticated == true &&
                (!Guid.TryParse(user.FindFirst("sub")?.Value, out var id) || id == Guid.Empty)))
        {
            context.Result = new ForbidResult();
            return;
        }
        await next();
    }
}
