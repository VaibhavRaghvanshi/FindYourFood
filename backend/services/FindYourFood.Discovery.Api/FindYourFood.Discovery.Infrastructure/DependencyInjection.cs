using FindYourFood.Discovery.Application.Interfaces;
using FindYourFood.Discovery.Application.Services;
using FindYourFood.Discovery.Infrastructure.Data;
using FindYourFood.Discovery.Infrastructure.Repositories;
using FindYourFood.Discovery.Infrastructure.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FindYourFood.Discovery.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddDiscovery(this IServiceCollection services, string connection)
    {
        services.AddDbContext<DiscoveryDbContext>(options => options.UseSqlServer(DiscoveryDbContext.OwnedConnection(connection)));
        services.AddScoped<ISearchRepository, SearchRepository>();
        services.AddScoped<IUnitOfWork, DiscoveryUnitOfWork>();
        services.AddScoped<RestaurantSearch>();
        services.AddSingleton(TimeProvider.System);
        return services;
    }
}
