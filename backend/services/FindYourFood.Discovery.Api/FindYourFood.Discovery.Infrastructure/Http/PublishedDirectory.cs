using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using FindYourFood.Discovery.Application.DTOs;
using FindYourFood.Discovery.Application.Interfaces;
using FindYourFood.Discovery.Domain;

namespace FindYourFood.Discovery.Infrastructure.Http;

public sealed class PublishedDirectory(HttpClient provider, HttpClient catalog) : IPublishedDirectory
{
    private sealed record MenuItem(string Name, bool Available, string[] Tags);
    private sealed record Menu(string Name, MenuItem[] Items);
    private sealed record Publication(long Version, Menu Menu);

    public async Task<SearchDocument[]> FetchAsync(string city, CancellationToken ct)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
        deadline.CancelAfter(TimeSpan.FromSeconds(12));
        var branches = new List<PublicBranch>();
        for (var page = 1; page <= 21; page++)
        {
            var batch = await provider.GetFromJsonAsync<PublicBranch[]>(
                $"/api/v1/providers/public/branches?city={Uri.EscapeDataString(city)}&page={page}", deadline.Token)
                ?? throw new SearchUnavailableException();
            branches.AddRange(batch);
            if (branches.Count > 1000) throw new SearchUnavailableException();
            if (batch.Length < 50) break;
        }

        var documents = new ConcurrentBag<SearchDocument>();
        await Parallel.ForEachAsync(branches.DistinctBy(branch => branch.Id),
            new ParallelOptions { MaxDegreeOfParallelism = 4, CancellationToken = deadline.Token },
            async (branch, cancellation) =>
            {
                using var response = await catalog.GetAsync(
                    $"/api/v1/catalog/branches/{branch.Id:D}", cancellation);
                if (response.StatusCode == HttpStatusCode.NotFound) return;
                response.EnsureSuccessStatusCode();
                var publication = await response.Content.ReadFromJsonAsync<Publication>(cancellation)
                    ?? throw new SearchUnavailableException();
                var available = publication.Menu.Items.Where(item => item.Available).ToArray();
                if (available.Length == 0) return;
                documents.Add(new(branch, publication.Menu.Name, available.Select(item => item.Name).ToArray(),
                    available.SelectMany(item => item.Tags).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
                    publication.Version));
            });
        return documents.ToArray();
    }
}
