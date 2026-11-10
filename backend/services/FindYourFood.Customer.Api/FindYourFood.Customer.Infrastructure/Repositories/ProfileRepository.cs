using FindYourFood.Customer.Application.Interfaces;
using FindYourFood.Customer.Domain;
using FindYourFood.Customer.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FindYourFood.Customer.Infrastructure.Repositories;

internal sealed class ProfileRepository(CustomerDbContext database) : IProfileRepository
{
    public Task<CustomerProfile?> FindAsync(Guid subject, CancellationToken ct) =>
        database.Profiles.SingleOrDefaultAsync(profile => profile.CustomerId == subject, ct);

    public void Add(CustomerProfile profile) => database.Profiles.Add(profile);
}
