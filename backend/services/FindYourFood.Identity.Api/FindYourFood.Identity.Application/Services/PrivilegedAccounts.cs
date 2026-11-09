using FindYourFood.Identity.Application.Interfaces;
using FindYourFood.Identity.Domain;

namespace FindYourFood.Identity.Application.Services;

public sealed class PrivilegedAccounts(IPrivilegedAccountRepository accounts)
{
    public Task<PrivilegedAccount?> EnrollAsync(string? email, string? password, string secret, string? code, bool owner)
    {
        var address = CustomerEmail.Parse(email);
        return address is null || password is null ||
               password.Length is < CustomerAccounts.MinimumPasswordLength or > CustomerAccounts.MaximumPasswordLength ||
               string.IsNullOrWhiteSpace(password) || !ValidCode(code)
            ? Task.FromResult<PrivilegedAccount?>(null)
            : accounts.EnrollAsync(address, password, secret, code!, owner);
    }

    public Task<PrivilegedAccount?> AuthenticateAsync(string? email, string? password, string? code)
    {
        var address = CustomerEmail.Parse(email);
        return address is null || password is not { Length: > 0 and <= CustomerAccounts.MaximumPasswordLength } ||
               !ValidCode(code)
            ? Task.FromResult<PrivilegedAccount?>(null)
            : accounts.AuthenticateAsync(address, password, code!);
    }

    private static bool ValidCode(string? code) => code is { Length: 6 } && code.All(char.IsAsciiDigit);
}
