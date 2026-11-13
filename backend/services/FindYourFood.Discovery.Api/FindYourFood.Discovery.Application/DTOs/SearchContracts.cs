using FindYourFood.Discovery.Domain;

namespace FindYourFood.Discovery.Application.DTOs;

public sealed record SearchRequest(string City, string? Query, string? Tag, bool OpenNow, int Page,
    double? Latitude, double? Longitude);
public sealed record SearchHit(Guid BranchId, string ProviderName, string BranchName, string Kind, string City,
    string Address, string MenuName, IReadOnlyList<string> Foods, IReadOnlyList<string> Tags, bool OpenNow,
    double? DistanceKm, bool? Serviceable, int? DeliveryFeeMinor, long MenuVersion);
public sealed record SearchResults(SearchHit[] Items, int Page, int Total, DateTimeOffset IndexedAt);
public sealed class SearchUnavailableException(Exception? inner = null) : Exception("Search refresh is unavailable.", inner);
