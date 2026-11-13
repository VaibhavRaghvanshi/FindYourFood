using System.Text.Json;
using FindYourFood.Discovery.Application.DTOs;
using FindYourFood.Discovery.Application.Interfaces;
using FindYourFood.Discovery.Domain;

namespace FindYourFood.Discovery.Application.Services;

public sealed class RestaurantSearch(ISearchRepository repository, IPublishedDirectory directory,
    IUnitOfWork unitOfWork, TimeProvider clock, IDeliveryCoverage coverage)
{
    public async Task<SearchResults> SearchAsync(SearchRequest request, CancellationToken ct)
    {
        var city = request.City.Trim().ToUpperInvariant();
        var now = clock.GetUtcNow();
        var index = await repository.IndexAsync(city, ct);
        var rows = await repository.CityAsync(city, ct);
        if (index is null || now - index.RefreshedAt > TimeSpan.FromMinutes(1))
        {
            var published = await directory.FetchAsync(request.City.Trim(), ct);
            index ??= NewIndex(city);
            var existing = rows.ToDictionary(row => row.BranchId);
            foreach (var document in published)
            {
                var json = JsonSerializer.Serialize(document);
                if (existing.Remove(document.Branch.Id, out var row)) row.Replace(json);
                else repository.Add(new BranchProjection(city, document.Branch.Id, json));
            }
            foreach (var row in existing.Values) repository.Remove(row);
            index.Refresh(now);
            if (!await unitOfWork.TrySaveAsync(ct)) throw new SearchUnavailableException();
            rows = await repository.CityAsync(city, ct);
        }

        var hits = rows.Select(row => JsonSerializer.Deserialize<SearchDocument>(row.DocumentJson)!)
            .Where(document => Matches(document, request))
            .Select(document => Hit(document, now))
            .Where(hit => !request.OpenNow || hit.OpenNow)
            .ToArray();
        if (request.Latitude is double latitude && request.Longitude is double longitude)
        {
            var documents = rows.Select(row => JsonSerializer.Deserialize<SearchDocument>(row.DocumentJson)!)
                .ToDictionary(document => document.Branch.Id);
            var positioned = hits.Where(hit => documents[hit.BranchId].Branch.Details.Latitude is not null).ToArray();
            if (positioned.Length > 0)
            {
                var origins = positioned.Select(hit => new GeoPoint(
                    documents[hit.BranchId].Branch.Details.Latitude!.Value,
                    documents[hit.BranchId].Branch.Details.Longitude!.Value)).ToArray();
                var quotes = await coverage.MatrixAsync(new(latitude, longitude), origins, ct);
                var byBranch = positioned.Select((hit, position) => (hit.BranchId, Quote: quotes[position]))
                    .ToDictionary(pair => pair.BranchId, pair => pair.Quote);
                hits = hits.Select(hit => byBranch.TryGetValue(hit.BranchId, out var quote)
                    ? hit with { Serviceable = quote.Serviceable, DeliveryFeeMinor = quote.DeliveryFeeMinor, DistanceKm = quote.DistanceKm }
                    : hit).ToArray();
            }
        }
        hits = hits.OrderByDescending(hit => hit.Serviceable == true)
            .ThenBy(hit => hit.DistanceKm ?? double.MaxValue).ThenByDescending(hit => hit.OpenNow)
            .ThenBy(hit => hit.ProviderName).ThenBy(hit => hit.BranchId).ToArray();
        return new(hits.Skip((request.Page - 1) * 20).Take(20).ToArray(), request.Page, hits.Length, index.RefreshedAt);
    }

    private CityIndex NewIndex(string city)
    {
        var index = new CityIndex(city);
        repository.Add(index);
        return index;
    }

    private static bool Matches(SearchDocument document, SearchRequest input)
    {
        var terms = input.Query?.Trim();
        var text = string.Join(' ', document.Foods.Prepend(document.MenuName)
            .Prepend(document.Branch.ProviderName).Prepend(document.Branch.Details.Name));
        return (string.IsNullOrEmpty(terms) || text.Contains(terms, StringComparison.OrdinalIgnoreCase)) &&
            (string.IsNullOrWhiteSpace(input.Tag) || document.Tags.Contains(input.Tag.Trim(), StringComparer.OrdinalIgnoreCase));
    }

    private static SearchHit Hit(SearchDocument document, DateTimeOffset now)
    {
        var branch = document.Branch;
        return new(branch.Id, branch.ProviderName, branch.Details.Name, branch.Kind, branch.Details.City,
            branch.Details.Address, document.MenuName, document.Foods, document.Tags, branch.IsOpen(now), null, null, null, document.MenuVersion);
    }
}
