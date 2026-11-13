using FindYourFood.Location.Application.Interfaces;
using FindYourFood.Location.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FindYourFood.Location.Infrastructure.UnitOfWork;

internal sealed class LocationUnitOfWork(LocationDbContext database) : IUnitOfWork
{
    public async Task<bool> TrySaveAsync(CancellationToken ct)
    {
        try
        {
            await database.SaveChangesAsync(ct);
            return true;
        }
        catch (Exception error) when (error is DbUpdateConcurrencyException ||
            error.GetBaseException() is SqlException { Number: 1205 or 2601 or 2627 })
        {
            return false;
        }
    }
}
