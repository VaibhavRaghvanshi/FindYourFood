using System.Net.Http.Headers;
using FindYourFood.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FindYourFood.Provider.Api.Authentication;

internal sealed class ProviderReviewFilter(OrganizationPermissionClient permissions) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var request = context.HttpContext.Request;
        if (!AuthenticationHeaderValue.TryParse(request.Headers.Authorization, out var authorization) ||
            !authorization.Scheme.Equals("Bearer", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(authorization.Parameter))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var scope = Guid.TryParse(request.RouteValues["id"]?.ToString(), out var id)
            ? $"/providers/{id:N}" : "/providers";
        var decision = await permissions.CheckAsync("provider.review", scope, authorization.Parameter, request.HttpContext.RequestAborted);
        if (decision == OrganizationPermissionDecision.Denied)
        {
            context.Result = new ForbidResult();
        }
        else if (decision == OrganizationPermissionDecision.Unavailable)
        {
            context.Result = new ObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "The permission service is unavailable."
            }) { StatusCode = StatusCodes.Status503ServiceUnavailable };
        }
    }
}
