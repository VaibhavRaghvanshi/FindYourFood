namespace FindYourFood.Identity.Domain;

public sealed record CustomerAccount(Guid Id, string Email, bool EmailVerified);
