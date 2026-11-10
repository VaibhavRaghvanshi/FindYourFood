using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FindYourFood.Experience.Application.Interfaces;

namespace FindYourFood.Experience.Infrastructure.Integrations;

internal sealed class OrganizationExperienceAccess(HttpClient http) : IExperienceAccess
{
    public async Task<AccessDecision> CheckAsync(string permission, string bearerToken, CancellationToken cancellationToken)
    {
        var scope = Uri.EscapeDataString(ExperiencePermissions.Scope);
        var action = Uri.EscapeDataString(permission);
        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"/api/v1/organization/permissions/check?permission={action}&scope={scope}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        try
        {
            using var response = await http.SendAsync(request, cancellationToken);
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                return AccessDecision.Denied;
            }

            if (!response.IsSuccessStatusCode)
            {
                return AccessDecision.Unavailable;
            }

            var decision = await response.Content.ReadFromJsonAsync<PermissionResponse>(cancellationToken);
            return decision?.Allowed == true ? AccessDecision.Allowed : AccessDecision.Denied;
        }
        catch (Exception error) when (error is HttpRequestException or JsonException ||
            (error is OperationCanceledException && !cancellationToken.IsCancellationRequested))
        {
            return AccessDecision.Unavailable;
        }
    }

    private sealed record PermissionResponse(bool Allowed);
}
