using FindYourFood.Ordering.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FindYourFood.Ordering.Api.Setup;

public sealed class OrderingDbContextFactory : IDesignTimeDbContextFactory<OrderingDbContext>
{
    public OrderingDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder().AddEnvironmentVariables().Build();
        var connection = OrderingDbContext.OwnedConnection(configuration.GetConnectionString("Ordering")
            ?? throw new InvalidOperationException("Configure ConnectionStrings:Ordering before running migrations."));
        return new OrderingDbContext(new DbContextOptionsBuilder<OrderingDbContext>().UseSqlServer(connection).Options);
    }
}
