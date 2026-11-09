using FindYourFood.Identity.Application.Interfaces;
using FindYourFood.Identity.Application.Services;
using FindYourFood.Identity.Infrastructure.Persistence;
using FindYourFood.Identity.Infrastructure.Repositories;
using FindYourFood.Identity.Infrastructure.Services;
using FindYourFood.Identity.Infrastructure.UnitOfWork;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FindYourFood.Identity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityPersistence(this IServiceCollection services, string connection)
    {
        var database = new SqlConnectionStringBuilder(connection);
        if (!string.Equals(database.InitialCatalog, "FindYourFood_IdentityDb", StringComparison.OrdinalIgnoreCase) &&
            !database.InitialCatalog.StartsWith("FindYourFood_IdentityTest_", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Identity requires its own FindYourFood_IdentityDb database.");
        }

        services.AddDbContext<IdentityDbContext>(options => options.UseSqlServer(connection));
        services.AddIdentityCore<CustomerUser>(options =>
        {
            options.User.AllowedUserNameCharacters = string.Empty;
            options.User.RequireUniqueEmail = true;
            options.Password.RequiredLength = CustomerAccounts.MinimumPasswordLength;
            options.Password.RequiredUniqueChars = 1;
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        }).AddEntityFrameworkStores<IdentityDbContext>().AddSignInManager();

        services.AddSingleton<UnknownCustomerPassword>();
        services.AddScoped<IUnitOfWork, IdentityUnitOfWork>();
        services.AddScoped<ICustomerAccountRepository, CustomerAccountRepository>();
        services.AddScoped<CustomerAccounts>();
        services.AddScoped<IPrivilegedAccountRepository, PrivilegedAccountRepository>();
        services.AddScoped<PrivilegedAccounts>();
        services.AddSingleton<IAuthenticatorEnrollment, AuthenticatorEnrollment>();
        return services;
    }
}
