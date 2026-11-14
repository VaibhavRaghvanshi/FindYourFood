namespace FindYourFood.Payment.Application.Interfaces;

public interface IUnitOfWork
{
    Task<bool> TrySaveAsync(CancellationToken ct);
}
