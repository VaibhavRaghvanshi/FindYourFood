namespace FindYourFood.Identity.Application.DTOs;

public sealed record AccessTokenResponse(string AccessToken, int ExpiresIn, string TokenType = "Bearer");
