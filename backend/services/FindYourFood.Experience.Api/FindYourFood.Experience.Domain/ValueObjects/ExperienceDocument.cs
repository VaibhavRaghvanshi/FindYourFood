namespace FindYourFood.Experience.Domain;

public sealed record ExperienceDocument(
    int SchemaVersion, string Brand, string Palette, string Typography, bool ShowNavigation,
    IReadOnlyList<ExperienceLink> Navigation, IReadOnlyList<ExperienceSection> Sections);

public sealed record ExperienceLink(string Target, string Label);

public sealed record ExperienceSection(string Kind, string Title, string Body);
