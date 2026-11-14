using FindYourFood.Payment.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FindYourFood.Payment.Api.Setup;

public sealed class PaymentDbContextFactory : IDesignTimeDbContextFactory<PaymentDbContext>
{
    public PaymentDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder().AddEnvironmentVariables().Build();
        var connection = PaymentDbContext.OwnedConnection(configuration.GetConnectionString("Payment")
            ?? throw new InvalidOperationException("Configure ConnectionStrings:Payment before running migrations."));
        return new PaymentDbContext(new DbContextOptionsBuilder<PaymentDbContext>().UseSqlServer(connection).Options);
    }
}
