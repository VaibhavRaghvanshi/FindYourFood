namespace FindYourFood.Identity.Domain;

public sealed record PrivilegedAccount(Guid Id, bool IsOwner);
