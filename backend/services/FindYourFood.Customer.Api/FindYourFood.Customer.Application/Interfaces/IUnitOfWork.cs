namespace FindYourFood.Customer.Application.Interfaces;

public interface IUnitOfWork
{
    Task<bool> TrySaveAsync(CancellationToken ct);
}
