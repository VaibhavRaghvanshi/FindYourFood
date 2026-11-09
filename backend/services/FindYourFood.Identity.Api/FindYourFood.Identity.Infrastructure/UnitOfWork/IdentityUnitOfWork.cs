using FindYourFood.Identity.Application.Interfaces;
using FindYourFood.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace FindYourFood.Identity.Infrastructure.UnitOfWork;

internal sealed class IdentityUnitOfWork(IdentityDbContext database) : IUnitOfWork
{
    public async Task<IUnitOfWorkTransaction> BeginTransactionAsync() =>
        new Transaction(await database.Database.BeginTransactionAsync());

    public Task SaveChangesAsync() => database.SaveChangesAsync();

    private sealed class Transaction(IDbContextTransaction transaction) : IUnitOfWorkTransaction
    {
        public Task CommitAsync() => transaction.CommitAsync();

        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}
