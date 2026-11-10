namespace FindYourFood.Experience.Domain;

public static class ExperienceRules
{
    public const int SchemaVersion = 1;

    public static bool IsValid(ExperienceDocument? document)
    {
        if (document is null || document.SchemaVersion != SchemaVersion ||
            !IsText(document.Brand, 40) ||
            document.Palette is not ("forest" or "plum" or "ocean") ||
            document.Typography is not ("editorial" or "system") ||
            document.Navigation is not { Count: <= 2 } || document.Sections is not { Count: > 0 and <= 3 })
        {
            return false;
        }

        var links = new HashSet<string>(StringComparer.Ordinal);
        foreach (var link in document.Navigation)
        {
            if (link is null || link.Target is not ("home" or "account") ||
                !IsText(link.Label, 30) || !links.Add(link.Target))
            {
                return false;
            }
        }

        var components = new HashSet<string>(StringComparer.Ordinal);
        foreach (var section in document.Sections)
        {
            if (section is null || section.Kind is not ("hero" or "banner" or "note") ||
                !IsText(section.Title, 80) || !IsText(section.Body, 280) || !components.Add(section.Kind))
            {
                return false;
            }
        }

        return components.Contains("hero");
    }

    private static bool IsText(string? value, int maximumLength) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= maximumLength && !value.Any(char.IsControl);
}
