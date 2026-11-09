using FindYourFood.Identity.Domain;

namespace FindYourFood.Identity.Application.Interfaces;

public interface IPrivilegedAccountRepository
{
    Task<PrivilegedAccount?> EnrollAsync(CustomerEmail email, string password, string secret, string code, bool owner);
    Task<PrivilegedAccount?> AuthenticateAsync(CustomerEmail email, string password, string code);
}
