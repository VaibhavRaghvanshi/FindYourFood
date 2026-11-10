using FindYourFood.Catalog.Application.Interfaces;
using FindYourFood.Catalog.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FindYourFood.Catalog.Infrastructure.UnitOfWork;

internal sealed class CatalogUnitOfWork(CatalogDbContext database) : IUnitOfWork
{
    public async Task SaveAsync(CancellationToken ct)
    {
        try
        {
            await database.SaveChangesAsync(ct);
        }
        catch (Exception error) when (error is DbUpdateConcurrencyException ||
            error.GetBaseException() is SqlException { Number: 1205 or 2601 or 2627 })
        {
            throw new MenuConflictException(error);
        }
    }
}
