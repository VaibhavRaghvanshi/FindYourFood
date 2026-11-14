using FindYourFood.Ordering.Application.Interfaces;
using FindYourFood.Ordering.Application.Services;
using FindYourFood.Ordering.Infrastructure.Data;
using FindYourFood.Ordering.Infrastructure.Repositories;
using FindYourFood.Ordering.Infrastructure.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using FindYourFood.Contracts;

namespace FindYourFood.Ordering.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddOrdering(this IServiceCollection services, string connection)
    {
        services.AddDbContext<OrderingDbContext>(options => options.UseSqlServer(OrderingDbContext.OwnedConnection(connection)));
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IUnitOfWork, OrderingUnitOfWork>();
        services.AddScoped<OrderCheckout>();
        services.AddScoped<IIntegrationEventHandler>(provider => provider.GetRequiredService<OrderCheckout>());
        services.AddSingleton(TimeProvider.System);
        return services;
    }
}
