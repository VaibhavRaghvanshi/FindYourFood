namespace FindYourFood.Cart.Application.Interfaces;

public interface IUnitOfWork
{
    Task<bool> TrySaveAsync(CancellationToken ct);
}
