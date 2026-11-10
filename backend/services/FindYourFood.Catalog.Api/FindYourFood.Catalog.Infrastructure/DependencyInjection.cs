using FindYourFood.Catalog.Application.Interfaces;
using FindYourFood.Catalog.Application.Services;
using FindYourFood.Catalog.Infrastructure.Data;
using FindYourFood.Catalog.Infrastructure.Repositories;
using FindYourFood.Catalog.Infrastructure.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FindYourFood.Catalog.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCatalog(this IServiceCollection services, string connection)
    {
        services.AddDbContext<CatalogDbContext>(options => options.UseSqlServer(CatalogDbContext.OwnedConnection(connection)));
        services.AddScoped<IMenuRepository, MenuRepository>();
        services.AddScoped<IUnitOfWork, CatalogUnitOfWork>();
        services.AddScoped<MenuPublishing>();
        services.AddSingleton(TimeProvider.System);
        return services;
    }
}
