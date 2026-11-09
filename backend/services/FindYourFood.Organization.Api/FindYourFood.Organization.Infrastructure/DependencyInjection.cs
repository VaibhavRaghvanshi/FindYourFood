using FindYourFood.Organization.Application.Interfaces;
using FindYourFood.Organization.Application.Services;
using FindYourFood.Organization.Infrastructure.Repositories;
using FindYourFood.Organization.Infrastructure.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FindYourFood.Organization.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddOrganizationPersistence(this IServiceCollection services, string connection)
    {
        var ownedConnection = OrganizationDbContext.OwnedConnection(connection);
        services.AddDbContext<OrganizationDbContext>(options => options.UseSqlServer(ownedConnection));
        services.AddScoped<IGrantRepository, GrantRepository>();
        services.AddScoped<IUnitOfWork, OrganizationUnitOfWork>();
        services.AddScoped<GrantAdministration>();
        return services;
    }
}
