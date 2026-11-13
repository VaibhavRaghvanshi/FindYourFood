using FindYourFood.Location.Application.Interfaces;
using FindYourFood.Location.Application.Services;
using FindYourFood.Location.Infrastructure.Data;
using FindYourFood.Location.Infrastructure.Repositories;
using FindYourFood.Location.Infrastructure.Routing;
using FindYourFood.Location.Infrastructure.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FindYourFood.Location.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddLocation(this IServiceCollection services, string connection)
    {
        services.AddDbContext<LocationDbContext>(options => options.UseSqlServer(LocationDbContext.OwnedConnection(connection)));
        services.AddScoped<IZoneRepository, ZoneRepository>();
        services.AddScoped<IUnitOfWork, LocationUnitOfWork>();
        services.AddScoped<DeliveryZones>();
        services.AddSingleton<IRouteEstimator, StraightLineEstimator>();
        services.AddSingleton(TimeProvider.System);
        return services;
    }
}
