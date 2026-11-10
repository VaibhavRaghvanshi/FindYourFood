using FindYourFood.Customer.Domain;

namespace FindYourFood.Customer.Application.Interfaces;

public interface IProfileRepository
{
    Task<CustomerProfile?> FindAsync(Guid subject, CancellationToken ct);
    void Add(CustomerProfile profile);
}
