using FindYourFood.Identity.Application.DTOs;
using FindYourFood.Identity.Application.Interfaces;
using FindYourFood.Identity.Domain;

namespace FindYourFood.Identity.Application.Services;

public sealed class CustomerAccounts(ICustomerAccountRepository accounts)
{
    public const int MinimumPasswordLength = 12;
    public const int MaximumPasswordLength = 128;

    public async Task<RegistrationResult> RegisterAsync(string? email, string? password)
    {
        var address = CustomerEmail.Parse(email);
        if (address is null || password is null ||
            password.Length is < MinimumPasswordLength or > MaximumPasswordLength)
        {
            return RegistrationResult.InvalidInput;
        }

        return await accounts.CreateAsync(address, password)
            ? RegistrationResult.Created
            : RegistrationResult.AlreadyRegistered;
    }

    public Task<CustomerAccount?> AuthenticateAsync(string? email, string? password)
    {
        var address = CustomerEmail.Parse(email);
        return address is null || password is not { Length: > 0 and <= MaximumPasswordLength }
            ? Task.FromResult<CustomerAccount?>(null)
            : accounts.AuthenticateAsync(address, password);
    }

    public Task<CustomerAccount?> FindAsync(Guid id) => accounts.FindAsync(id);
}
