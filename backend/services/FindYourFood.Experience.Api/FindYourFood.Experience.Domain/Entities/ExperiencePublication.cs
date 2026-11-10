namespace FindYourFood.Experience.Domain;

public sealed record ExperiencePublication(
    int Version, string ConfigurationJson, Guid PublishedBy, DateTimeOffset PublishedAt,
    Guid? ApprovedBy, int? RestoredFromVersion);
