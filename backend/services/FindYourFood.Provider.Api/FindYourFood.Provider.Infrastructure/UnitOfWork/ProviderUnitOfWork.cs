using System.Data;
using FindYourFood.Provider.Application.Interfaces;
using FindYourFood.Provider.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FindYourFood.Provider.Infrastructure.UnitOfWork;

internal sealed class ProviderUnitOfWork(ProviderDbContext database) : IUnitOfWork
{
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken ct)
    {
        try
        {
            await using var transaction = await database.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
            var result = await action();
            await transaction.CommitAsync(ct);
            return result;
        }
        catch (Exception error) when (error is DbUpdateConcurrencyException ||
            error.GetBaseException() is SqlException { Number: 1205 or 1222 or 2601 or 2627 })
        {
            throw new ProviderContentionException(error);
        }
    }

    public Task SaveChangesAsync(CancellationToken ct) => database.SaveChangesAsync(ct);
}
