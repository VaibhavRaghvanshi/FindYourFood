using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace FindYourFood.Provider.Domain;

public static class ProviderRules
{
    public const int MaxDocumentBytes = 2_000_000;
    public const int MaxBranches = 20;
    private static readonly byte[] PngSignature = [137, 80, 78, 71, 13, 10, 26, 10];
    private static readonly byte[] JpegSignature = [255, 216, 255];

    public static bool ValidDetails(ProviderDetails? details) =>
        details is not null && Text(details.Name, 100) &&
        details.Kind is "restaurant" or "dhaba" or "cloud-kitchen" or "cafe" or "bakery" or "home-kitchen" or "tiffin" &&
        Text(details.ContactEmail, 254) && new EmailAddressAttribute().IsValid(details.ContactEmail) &&
        Text(details.Description, 400);

    public static bool ValidDocumentKind(string kind) => kind is "license" or "address-proof";

    public static string? DetectMediaType(ReadOnlySpan<byte> content)
    {
        if (content.Length > MaxDocumentBytes) return null;
        if (content.StartsWith("%PDF-"u8)) return "application/pdf";
        if (content.StartsWith(PngSignature)) return "image/png";
        return content.StartsWith(JpegSignature) ? "image/jpeg" : null;
    }

    public static bool ValidBranch(BranchDetails? branch, DateTimeOffset now)
    {
        if (branch is null || !Text(branch.Name, 100) || !Text(branch.Address, 240) ||
            !Text(branch.City, 80) || !Text(branch.TimeZone, 80) ||
            !TimeZoneInfo.TryFindSystemTimeZoneById(branch.TimeZone, out _) ||
            branch.Hours is not { Count: > 0 and <= 7 } || branch.ClosedUntil > now.AddDays(90) ||
            !((branch.Latitude is null && branch.Longitude is null) ||
                (branch.Latitude is >= -90 and <= 90 && branch.Longitude is >= -180 and <= 180)))
        {
            return false;
        }

        var days = new HashSet<int>();
        foreach (var day in branch.Hours)
        {
            if (day is null || day.Day is < 1 or > 7 || !days.Add(day.Day) ||
                !TimeOnly.TryParseExact(day.Opens, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var opens) ||
                !TimeOnly.TryParseExact(day.Closes, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var closes) ||
                closes <= opens)
            {
                return false;
            }
        }

        return true;
    }

    public static bool ValidReview(bool approved, string? note) =>
        (approved && string.IsNullOrEmpty(note)) || Text(note, 300);

    private static bool Text(string? value, int max) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= max && !value.Any(char.IsControl);
}
