using System.Data;
using FindYourFood.Experience.Application.Interfaces;
using FindYourFood.Experience.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FindYourFood.Experience.Infrastructure.UnitOfWork;

internal sealed class ExperienceUnitOfWork(ExperienceDbContext database) : IUnitOfWork
{
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken)
    {
        try
        {
            await using var transaction = await database.Database
                .BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
            var result = await operation();
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch (Exception error) when (error is DbUpdateConcurrencyException ||
            error.GetBaseException() is SqlException { Number: 1205 or 1222 or 2601 or 2627 })
        {
            throw new ExperienceContentionException(error);
        }
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => database.SaveChangesAsync(cancellationToken);
}
