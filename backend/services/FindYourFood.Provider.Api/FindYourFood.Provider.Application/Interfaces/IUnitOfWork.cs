namespace FindYourFood.Provider.Application.Interfaces;

public interface IUnitOfWork
{
    Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed class ProviderContentionException(Exception inner) : Exception("The provider profile changed concurrently.", inner);
