using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace FindYourFood.Hosting;

public enum OrganizationPermissionDecision { Allowed, Denied, Unavailable }

public sealed class OrganizationPermissionClient(HttpClient http)
{
    public async Task<OrganizationPermissionDecision> CheckAsync(
        string permission, string scope, string bearerToken, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"/api/v1/organization/permissions/check?permission={Uri.EscapeDataString(permission)}&scope={Uri.EscapeDataString(scope)}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        try
        {
            using var response = await http.SendAsync(request, ct);
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                return OrganizationPermissionDecision.Denied;
            }

            if (!response.IsSuccessStatusCode)
            {
                return OrganizationPermissionDecision.Unavailable;
            }

            var result = await response.Content.ReadFromJsonAsync<PermissionResponse>(ct);
            return result?.Allowed == true ? OrganizationPermissionDecision.Allowed : OrganizationPermissionDecision.Denied;
        }
        catch (Exception error) when (error is HttpRequestException or JsonException ||
            (error is OperationCanceledException && !ct.IsCancellationRequested))
        {
            return OrganizationPermissionDecision.Unavailable;
        }
    }

    private sealed record PermissionResponse(bool Allowed);
}

public static class OrganizationPermissionRegistration
{
    public static IServiceCollection AddOrganizationPermissionClient(this IServiceCollection services, Uri organization)
    {
        services.AddHttpClient<OrganizationPermissionClient>(client =>
        {
            client.BaseAddress = organization;
            client.Timeout = TimeSpan.FromSeconds(3);
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
        return services;
    }
}
