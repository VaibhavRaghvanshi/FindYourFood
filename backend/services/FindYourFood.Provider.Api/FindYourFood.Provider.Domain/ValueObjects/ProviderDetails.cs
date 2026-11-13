namespace FindYourFood.Provider.Domain;

public sealed record ProviderDetails(string Name, string Kind, string ContactEmail, string Description);

public sealed record BranchDetails(
    string Name, string Address, string City, string TimeZone, IReadOnlyList<OpeningWindow> Hours,
    DateTimeOffset? ClosedUntil, double? Latitude = null, double? Longitude = null);

public sealed record OpeningWindow(int Day, string Opens, string Closes);
