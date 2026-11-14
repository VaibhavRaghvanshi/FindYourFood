using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FindYourFood.Hosting;

public sealed class CustomerRequestFilter : IAsyncActionFilter
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
