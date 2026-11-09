namespace FindYourFood.Identity.Application.Interfaces;

public interface IUnitOfWork
{
    Task<IUnitOfWorkTransaction> BeginTransactionAsync();
    Task SaveChangesAsync();
}

public interface IUnitOfWorkTransaction : IAsyncDisposable
{
    Task CommitAsync();
}
