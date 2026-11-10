using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FindYourFood.Catalog.Api.Authentication;

internal sealed class BranchOwnerFilter(IHttpClientFactory clients) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var request = context.HttpContext.Request;
        if (!Guid.TryParse(request.RouteValues["branch"]?.ToString(), out var branch) ||
            !AuthenticationHeaderValue.TryParse(request.Headers.Authorization, out var authorization))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        using var lookup = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/providers/me/branches/{branch:D}/access");
        lookup.Headers.Authorization = authorization;
        var ct = context.HttpContext.RequestAborted;
        try
        {
            using var response = await clients.CreateClient("provider").SendAsync(lookup, ct);
            if (response.StatusCode == HttpStatusCode.NoContent) return;
            context.Result = response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized
                ? new ForbidResult()
                : new StatusCodeResult(StatusCodes.Status503ServiceUnavailable);
        }
        catch (Exception error) when (error is HttpRequestException ||
            (error is OperationCanceledException && !ct.IsCancellationRequested))
        {
            context.Result = new StatusCodeResult(StatusCodes.Status503ServiceUnavailable);
        }
    }
}
