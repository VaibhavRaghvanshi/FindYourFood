namespace FindYourFood.Ordering.Application.Interfaces;

public interface IUnitOfWork
{
    Task<bool> TrySaveAsync(CancellationToken ct);
}
