namespace FindYourFood.Experience.Application.Interfaces;

public interface IUnitOfWork
{
    Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed class ExperienceContentionException(Exception inner) : Exception("The experience changed concurrently.", inner);
