using FindYourFood.Customer.Application.Interfaces;
using FindYourFood.Customer.Application.Services;
using FindYourFood.Customer.Infrastructure.Data;
using FindYourFood.Customer.Infrastructure.Repositories;
using FindYourFood.Customer.Infrastructure.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FindYourFood.Customer.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCustomer(this IServiceCollection services, string connection)
    {
        services.AddDbContext<CustomerDbContext>(options => options.UseSqlServer(CustomerDbContext.OwnedConnection(connection)));
        services.AddScoped<IProfileRepository, ProfileRepository>();
        services.AddScoped<IUnitOfWork, CustomerUnitOfWork>();
        services.AddScoped<CustomerProfiles>();
        services.AddSingleton(TimeProvider.System);
        return services;
    }
}
