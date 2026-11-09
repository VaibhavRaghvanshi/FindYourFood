namespace FindYourFood.Identity.Application.DTOs;

public sealed record CustomerProfile(Guid Id, string Email, bool EmailVerified);
