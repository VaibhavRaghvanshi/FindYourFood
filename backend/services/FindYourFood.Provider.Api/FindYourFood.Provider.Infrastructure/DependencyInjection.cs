using FindYourFood.Provider.Application.Interfaces;
using FindYourFood.Provider.Application.Services;
using FindYourFood.Provider.Infrastructure.Data;
using FindYourFood.Provider.Infrastructure.Repositories;
using FindYourFood.Provider.Infrastructure.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FindYourFood.Provider.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddProvider(this IServiceCollection services, string connection)
    {
        var ownedConnection = ProviderDbContext.OwnedConnection(connection);
        services.AddDbContext<ProviderDbContext>(options => options.UseSqlServer(ownedConnection));
        services.AddScoped<IProviderRepository, ProviderRepository>();
        services.AddScoped<IUnitOfWork, ProviderUnitOfWork>();
        services.AddScoped<ProviderOnboarding>();
        services.AddSingleton(TimeProvider.System);
        return services;
    }
}
