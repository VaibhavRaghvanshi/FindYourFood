using FindYourFood.Identity.Domain;

namespace FindYourFood.Identity.Application;

public interface IPrivilegedAccountStore
{
    Task<PrivilegedAccount?> EnrollAsync(CustomerEmail email, string password, string secret, string code, bool owner);
    Task<PrivilegedAccount?> AuthenticateAsync(CustomerEmail email, string password, string code);
}

public interface IAuthenticatorEnrollment
{
    string CreateSecret();
}

public sealed class PrivilegedAccounts(IPrivilegedAccountStore store)
{
    public Task<PrivilegedAccount?> EnrollAsync(string? email, string? password, string secret, string? code, bool owner)
    {
        var address = CustomerEmail.Parse(email);
        return address is null || password is null ||
               password.Length is < CustomerAccounts.MinimumPasswordLength or > CustomerAccounts.MaximumPasswordLength ||
               string.IsNullOrWhiteSpace(password) || !ValidCode(code)
            ? Task.FromResult<PrivilegedAccount?>(null)
            : store.EnrollAsync(address, password, secret, code!, owner);
    }

    public Task<PrivilegedAccount?> AuthenticateAsync(string? email, string? password, string? code)
    {
        var address = CustomerEmail.Parse(email);
        return address is null || password is not { Length: > 0 and <= CustomerAccounts.MaximumPasswordLength } ||
               !ValidCode(code)
            ? Task.FromResult<PrivilegedAccount?>(null)
            : store.AuthenticateAsync(address, password, code!);
    }

    private static bool ValidCode(string? code) => code is { Length: 6 } && code.All(char.IsAsciiDigit);
}
