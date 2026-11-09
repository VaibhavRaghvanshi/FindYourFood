using FindYourFood.Identity.Domain;

namespace FindYourFood.Identity.Application;

public interface ICustomerAccountStore
{
    Task<bool> CreateAsync(CustomerEmail email, string password);
    Task<CustomerAccount?> AuthenticateAsync(CustomerEmail email, string password);
    Task<CustomerAccount?> FindAsync(Guid id);
}

public enum RegistrationResult
{
    Created,
    InvalidInput,
    AlreadyRegistered
}

public sealed class CustomerAccounts(ICustomerAccountStore store)
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

        return await store.CreateAsync(address, password)
            ? RegistrationResult.Created
            : RegistrationResult.AlreadyRegistered;
    }

    public Task<CustomerAccount?> AuthenticateAsync(string? email, string? password)
    {
        var address = CustomerEmail.Parse(email);
        return address is null || password is not { Length: > 0 and <= MaximumPasswordLength }
            ? Task.FromResult<CustomerAccount?>(null)
            : store.AuthenticateAsync(address, password);
    }

    public Task<CustomerAccount?> FindAsync(Guid id) => store.FindAsync(id);
}
