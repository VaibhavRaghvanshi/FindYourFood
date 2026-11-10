namespace FindYourFood.Provider.Domain;

public sealed record ProviderReview(
    Guid Id, Guid ProviderId, int ApplicationRevision, Guid ReviewerId,
    bool Approved, string? Note, DateTimeOffset ReviewedAt);
