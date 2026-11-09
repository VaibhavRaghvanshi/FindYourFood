using FindYourFood.Identity.Application;
using FindYourFood.Identity.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FindYourFood.Identity.Infrastructure.Persistence;

internal sealed class AspNetCustomerAccountStore(
    UserManager<CustomerUser> users,
    SignInManager<CustomerUser> signIn,
    UnknownCustomerPassword unknownCustomer) : ICustomerAccountStore
{
    public async Task<bool> CreateAsync(CustomerEmail email, string password)
    {
        var user = new CustomerUser { Id = Guid.NewGuid(), UserName = email.Value, Email = email.Value };
        try
        {
            var result = await users.CreateAsync(user, password);
            if (result.Succeeded)
            {
                return true;
            }

            if (result.Errors.All(error => error.Code is "DuplicateUserName" or "DuplicateEmail"))
            {
                return false;
            }

            throw new InvalidOperationException("Customer account creation failed Identity validation.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            // The unique normalized username index also protects concurrent registrations.
            return false;
        }
    }

    public async Task<CustomerAccount?> AuthenticateAsync(CustomerEmail email, string password)
    {
        var user = await users.FindByNameAsync(email.Value);
        if (user is null)
        {
            unknownCustomer.Verify(password);
            return null;
        }

        var result = await signIn.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
        return result.Succeeded ? ToAccount(user) : null;
    }

    public async Task<CustomerAccount?> FindAsync(Guid id)
    {
        var user = await users.FindByIdAsync(id.ToString());
        return user is null ? null : ToAccount(user);
    }

    private static CustomerAccount ToAccount(CustomerUser user) =>
        new(user.Id, user.Email!, user.EmailConfirmed);
}

internal sealed class UnknownCustomerPassword
{
    private readonly CustomerUser _user = new();
    private readonly PasswordHasher<CustomerUser> _hasher = new();
    private readonly string _hash;

    public UnknownCustomerPassword() =>
        _hash = _hasher.HashPassword(_user, Guid.NewGuid().ToString());

    public void Verify(string password) => _hasher.VerifyHashedPassword(_user, _hash, password);
}
