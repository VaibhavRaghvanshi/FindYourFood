using FindYourFood.Organization.Application;

namespace FindYourFood.Organization.Api.Middleware;

internal sealed class GrantContentionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers.CacheControl = "no-store";
        try
        {
            await next(context);
        }
        catch (GrantContentionException)
        {
            await Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Permission records are busy. Retry the request.").ExecuteAsync(context);
        }
    }
}
