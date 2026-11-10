namespace FindYourFood.Catalog.Application.Interfaces;

public interface IUnitOfWork
{
    Task SaveAsync(CancellationToken ct);
}

public sealed class MenuConflictException(Exception inner) : Exception("The menu changed concurrently.", inner);
