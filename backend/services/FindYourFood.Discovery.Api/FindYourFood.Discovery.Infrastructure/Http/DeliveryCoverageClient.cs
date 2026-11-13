using System.Net.Http.Json;
using FindYourFood.Discovery.Application.DTOs;
using FindYourFood.Discovery.Application.Interfaces;

namespace FindYourFood.Discovery.Infrastructure.Http;

public sealed class DeliveryCoverageClient(HttpClient client) : IDeliveryCoverage
{
    public async Task<DeliveryCoverage[]> MatrixAsync(GeoPoint destination, GeoPoint[] origins, CancellationToken ct)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/v1/location/coverage-matrix", new { destination, origins }, ct);
        response.EnsureSuccessStatusCode();
        var matrix = await response.Content.ReadFromJsonAsync<DeliveryCoverage[]>(ct);
        if (matrix?.Length != origins.Length) throw new SearchUnavailableException();
        return matrix;
    }
}
