using System.Globalization;

namespace FindYourFood.Discovery.Domain;

public sealed record OpeningWindow(int Day, string Opens, string Closes);
public sealed record PublicBranchDetails(string Name, string Address, string City, string TimeZone,
    IReadOnlyList<OpeningWindow> Hours, DateTimeOffset? ClosedUntil, double? Latitude, double? Longitude);
public sealed record PublicBranch(Guid Id, string ProviderName, string Kind, PublicBranchDetails Details)
{
    public bool IsOpen(DateTimeOffset now)
    {
        if (Details.ClosedUntil > now || !TimeZoneInfo.TryFindSystemTimeZoneById(Details.TimeZone, out var zone)) return false;
        var local = TimeZoneInfo.ConvertTime(now, zone);
        var day = ((int)local.DayOfWeek + 6) % 7 + 1;
        var time = TimeOnly.FromDateTime(local.DateTime);
        return Details.Hours.Any(window => window.Day == day &&
            TimeOnly.TryParseExact(window.Opens, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var opens) &&
            TimeOnly.TryParseExact(window.Closes, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var closes) &&
            time >= opens && time < closes);
    }
}

public sealed record SearchDocument(PublicBranch Branch, string MenuName, IReadOnlyList<string> Foods,
    IReadOnlyList<string> Tags, long MenuVersion);
