using FindYourFood.Customer.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FindYourFood.Customer.Api.Setup;

public sealed class CustomerDbContextFactory : IDesignTimeDbContextFactory<CustomerDbContext>
{
    public CustomerDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder().AddEnvironmentVariables().Build();
        var connection = CustomerDbContext.OwnedConnection(configuration.GetConnectionString("Customer")
            ?? throw new InvalidOperationException("Configure ConnectionStrings:Customer before running migrations."));
        return new CustomerDbContext(new DbContextOptionsBuilder<CustomerDbContext>().UseSqlServer(connection).Options);
    }
}
