using FindYourFood.Identity.Domain;

namespace FindYourFood.Identity.Application.Interfaces;

public interface ICustomerAccountRepository
{
    Task<bool> CreateAsync(CustomerEmail email, string password);
    Task<CustomerAccount?> AuthenticateAsync(CustomerEmail email, string password);
    Task<CustomerAccount?> FindAsync(Guid id);
}
