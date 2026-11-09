using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FindYourFood.Identity.Infrastructure.Persistence;

public sealed class CustomerUser : IdentityUser<Guid>;

public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options)
    : IdentityUserContext<CustomerUser, Guid>(options);
