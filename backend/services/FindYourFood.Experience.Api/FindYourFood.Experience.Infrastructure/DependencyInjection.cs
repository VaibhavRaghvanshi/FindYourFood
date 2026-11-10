using FindYourFood.Experience.Application.Interfaces;
using FindYourFood.Experience.Application.Services;
using FindYourFood.Experience.Infrastructure.Caching;
using FindYourFood.Experience.Infrastructure.Data;
using FindYourFood.Experience.Infrastructure.Integrations;
using FindYourFood.Experience.Infrastructure.Repositories;
using FindYourFood.Experience.Infrastructure.UnitOfWork;
using FindYourFood.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FindYourFood.Experience.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddExperience(this IServiceCollection services, string connection, Uri organization)
    {
        var ownedConnection = ExperienceDbContext.OwnedConnection(connection);
        services.AddDbContext<ExperienceDbContext>(options => options.UseSqlServer(ownedConnection));
        services.AddScoped<IExperienceRepository, ExperienceRepository>();
        services.AddScoped<IUnitOfWork, ExperienceUnitOfWork>();
        services.AddScoped<ExperiencePublishing>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IPublishedExperienceCache, PublishedExperienceCache>();
        services.AddOrganizationPermissionClient(organization);
        services.AddScoped<IExperienceAccess, OrganizationExperienceAccess>();
        return services;
    }
}
