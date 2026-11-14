using FindYourFood.Payment.Application.Interfaces;
using FindYourFood.Payment.Application.Services;
using FindYourFood.Payment.Infrastructure.Data;
using FindYourFood.Payment.Infrastructure.Repositories;
using FindYourFood.Payment.Infrastructure.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using FindYourFood.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace FindYourFood.Payment.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPayment(this IServiceCollection services, string connection)
    {
        services.AddDbContext<PaymentDbContext>(options => options.UseSqlServer(PaymentDbContext.OwnedConnection(connection)));
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IUnitOfWork, PaymentUnitOfWork>();
        services.AddScoped<Payments>();
        services.AddScoped<IIntegrationEventHandler>(provider => provider.GetRequiredService<Payments>());
        services.AddSingleton(TimeProvider.System);
        return services;
    }
}
