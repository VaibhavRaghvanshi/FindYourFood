using FindYourFood.Experience.Application.Interfaces;
using FindYourFood.Hosting;

namespace FindYourFood.Experience.Infrastructure.Integrations;

internal sealed class OrganizationExperienceAccess(OrganizationPermissionClient client) : IExperienceAccess
{
    public async Task<AccessDecision> CheckAsync(string permission, string bearerToken, CancellationToken cancellationToken)
    {
        var decision = await client.CheckAsync(permission, ExperiencePermissions.Scope, bearerToken, cancellationToken);
        return decision switch
        {
            OrganizationPermissionDecision.Allowed => AccessDecision.Allowed,
            OrganizationPermissionDecision.Denied => AccessDecision.Denied,
            _ => AccessDecision.Unavailable
        };
    }
}
