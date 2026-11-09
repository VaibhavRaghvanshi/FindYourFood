namespace FindYourFood.Organization.Domain;

public enum StaffLevel
{
    Ceo = 1,
    DepartmentHead,
    CityManager,
    TeamManager,
    Employee
}

public sealed record StaffActor(Guid Id, bool IsOwner);

public sealed record GrantRequest(
    Guid SubjectId, StaffLevel Level, string Permission, string Scope, bool CanDelegate, Guid? ParentGrantId);

public sealed record AccessGrant(
    Guid Id, Guid SubjectId, StaffLevel Level, string Permission, string Scope, bool CanDelegate,
    Guid? ParentGrantId, Guid GrantedBy, DateTimeOffset GrantedAt)
{
    public DateTimeOffset? RevokedAt { get; private set; }

    public void Revoke(DateTimeOffset now) => RevokedAt ??= now;
}

public static class StaffAccess
{
    public static bool IsKnownPermission(string? permission) => permission is
        "provider.review" or "support.ticket.read" or "support.ticket.assign" or "finance.report.read";

    public static bool IsScope(string? scope)
    {
        if (scope == "/")
        {
            return true;
        }

        return scope is { Length: > 1 and <= 160 } && scope[0] == '/' &&
               scope[1..].Split('/') is { Length: <= 8 } parts &&
               parts.All(part => part.Length is > 0 and <= 32 &&
                   part.All(character => character is >= 'a' and <= 'z' or >= '0' and <= '9' or '-'));
    }

    public static bool Covers(string parent, string child) =>
        parent == "/" || child == parent || child.StartsWith(parent + "/", StringComparison.Ordinal);

    public static bool CanDelegate(AccessGrant parent, GrantRequest request) =>
        parent.RevokedAt is null && parent.CanDelegate && parent.Level < request.Level &&
        parent.Permission == request.Permission && Covers(parent.Scope, request.Scope);
}
