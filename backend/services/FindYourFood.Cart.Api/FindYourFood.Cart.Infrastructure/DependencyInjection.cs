using FindYourFood.Cart.Application.Interfaces;
using FindYourFood.Cart.Application.Services;
using FindYourFood.Cart.Infrastructure.Data;
using FindYourFood.Cart.Infrastructure.Repositories;
using FindYourFood.Cart.Infrastructure.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FindYourFood.Cart.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCart(this IServiceCollection services, string connection)
    {
        services.AddDbContext<CartDbContext>(options => options.UseSqlServer(CartDbContext.OwnedConnection(connection)));
        services.AddScoped<ICartRepository, CartRepository>();
        services.AddScoped<IUnitOfWork, CartUnitOfWork>();
        services.AddScoped<ShoppingCarts>();
        services.AddSingleton(TimeProvider.System);
        return services;
    }
}
