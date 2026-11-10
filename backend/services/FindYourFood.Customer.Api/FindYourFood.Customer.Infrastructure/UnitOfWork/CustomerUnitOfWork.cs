using FindYourFood.Customer.Application.Interfaces;
using FindYourFood.Customer.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FindYourFood.Customer.Infrastructure.UnitOfWork;

internal sealed class CustomerUnitOfWork(CustomerDbContext database) : IUnitOfWork
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
