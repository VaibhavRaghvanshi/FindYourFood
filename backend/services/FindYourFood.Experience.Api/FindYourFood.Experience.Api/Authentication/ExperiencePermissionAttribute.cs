using System.Net.Http.Headers;
using System.Security.Claims;
using FindYourFood.Experience.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FindYourFood.Experience.Api.Authentication;

[AttributeUsage(AttributeTargets.Method)]
internal sealed class ExperiencePermissionAttribute : TypeFilterAttribute
{
    public ExperiencePermissionAttribute(params string[] permissions) : base(typeof(ExperiencePermissionFilter))
    {
        Arguments = [permissions];
    }
}

internal sealed class ExperiencePermissionFilter(string[] permissions, IExperienceAccess access) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var http = context.HttpContext;
        if (!Guid.TryParse(http.User.FindFirstValue("sub"), out var actor) || actor == Guid.Empty ||
            !AuthenticationHeaderValue.TryParse(http.Request.Headers.Authorization, out var authorization) ||
            !authorization.Scheme.Equals("Bearer", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(authorization.Parameter))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        foreach (var permission in permissions)
        {
            var decision = await access.CheckAsync(permission, authorization.Parameter, http.RequestAborted);
            if (decision == AccessDecision.Allowed)
            {
                return;
            }

            if (decision == AccessDecision.Unavailable)
            {
                context.Result = new ObjectResult(new ProblemDetails
                {
                    Status = StatusCodes.Status503ServiceUnavailable,
                    Title = "The permission service is unavailable."
                }) { StatusCode = StatusCodes.Status503ServiceUnavailable };
                return;
            }
        }

        context.Result = new ForbidResult();
    }
}
