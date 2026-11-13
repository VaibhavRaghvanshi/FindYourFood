using FindYourFood.Cart.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FindYourFood.Cart.Api.Setup;

public sealed class CartDbContextFactory : IDesignTimeDbContextFactory<CartDbContext>
{
    public CartDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder().AddEnvironmentVariables().Build();
        var connection = CartDbContext.OwnedConnection(configuration.GetConnectionString("Cart")
            ?? throw new InvalidOperationException("Configure ConnectionStrings:Cart before running migrations."));
        return new CartDbContext(new DbContextOptionsBuilder<CartDbContext>().UseSqlServer(connection).Options);
    }
}
