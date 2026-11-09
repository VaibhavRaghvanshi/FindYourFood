using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FindYourFood.Identity.Infrastructure.Persistence;

public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options)
    : IdentityUserContext<CustomerUser, Guid>(options)
{
    public DbSet<PrivilegedEnrollment> PrivilegedEnrollments => Set<PrivilegedEnrollment>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        var enrollment = builder.Entity<PrivilegedEnrollment>();
        enrollment.HasKey(account => account.UserId);
        enrollment.HasIndex(account => account.IsOwner).IsUnique().HasFilter("[IsOwner] = 1");
        enrollment.HasOne<CustomerUser>().WithOne().HasForeignKey<PrivilegedEnrollment>(account => account.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
