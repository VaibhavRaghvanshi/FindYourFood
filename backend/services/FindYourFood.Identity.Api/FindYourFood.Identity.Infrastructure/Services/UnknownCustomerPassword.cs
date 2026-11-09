using FindYourFood.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace FindYourFood.Identity.Infrastructure.Services;

internal sealed class UnknownCustomerPassword
{
    private readonly CustomerUser _user = new();
    private readonly PasswordHasher<CustomerUser> _hasher = new();
    private readonly string _hash;

    public UnknownCustomerPassword() =>
        _hash = _hasher.HashPassword(_user, Guid.NewGuid().ToString());

    public void Verify(string password) => _hasher.VerifyHashedPassword(_user, _hash, password);
}
