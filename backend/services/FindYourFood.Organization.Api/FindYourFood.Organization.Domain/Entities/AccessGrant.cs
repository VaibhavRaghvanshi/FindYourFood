namespace FindYourFood.Organization.Domain;

public sealed record AccessGrant(
    Guid Id, Guid SubjectId, StaffLevel Level, string Permission, string Scope, bool CanDelegate,
    Guid? ParentGrantId, Guid GrantedBy, DateTimeOffset GrantedAt)
{
    public DateTimeOffset? RevokedAt { get; private set; }

    public void Revoke(DateTimeOffset now) => RevokedAt ??= now;
}
