namespace FindYourFood.Identity.Infrastructure.Persistence;

public sealed class PrivilegedEnrollment
{
    public Guid UserId { get; init; }
    public bool IsOwner { get; init; }
    public string ProtectedAuthenticator { get; init; } = string.Empty;
    public long LastAcceptedStep { get; set; }
}
