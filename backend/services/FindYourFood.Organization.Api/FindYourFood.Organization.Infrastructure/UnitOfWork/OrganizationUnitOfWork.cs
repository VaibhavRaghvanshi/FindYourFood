using System.Data;
using FindYourFood.Organization.Application;
using FindYourFood.Organization.Application.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FindYourFood.Organization.Infrastructure.UnitOfWork;

internal sealed class OrganizationUnitOfWork(OrganizationDbContext database) : IUnitOfWork
{
    public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken)
    {
        try
        {
            await using var transaction = await database.Database
                .BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var result = await operation();
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch (Exception error) when (error is SqlException { Number: 1205 or 1222 } ||
            error.InnerException is SqlException { Number: 1205 or 1222 })
        {
            throw new GrantContentionException(error);
        }
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => database.SaveChangesAsync(cancellationToken);
}
