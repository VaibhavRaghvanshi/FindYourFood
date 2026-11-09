namespace FindYourFood.Organization.Domain;

public sealed record GrantRequest(
    Guid SubjectId, StaffLevel Level, string Permission, string Scope, bool CanDelegate, Guid? ParentGrantId);
