using FindYourFood.Identity.Application;
using FindYourFood.Identity.Domain;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using OtpNet;

namespace FindYourFood.Identity.Infrastructure.Persistence;

internal sealed class AuthenticatorEnrollment : IAuthenticatorEnrollment
{
    public string CreateSecret() => Base32Encoding.ToString(KeyGeneration.GenerateRandomKey(20));
}

internal sealed class PrivilegedAccountStore(
    IdentityDbContext database,
    UserManager<CustomerUser> users,
    IDataProtectionProvider protection,
    UnknownCustomerPassword unknownPassword,
    TimeProvider clock) : IPrivilegedAccountStore
{
    public async Task<PrivilegedAccount?> EnrollAsync(
        CustomerEmail email, string password, string secret, string code, bool owner)
    {
        if (!TryVerify(secret, code, out var step))
        {
            return null;
        }

        await using var transaction = await database.Database.BeginTransactionAsync();
        if (owner && await database.PrivilegedEnrollments.AnyAsync(enrollment => enrollment.IsOwner))
        {
            return null;
        }

        var user = new CustomerUser { Id = Guid.NewGuid(), UserName = email.Value, Email = email.Value };
        try
        {
            var result = await users.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                return null;
            }

            database.PrivilegedEnrollments.Add(new PrivilegedEnrollment
            {
                UserId = user.Id,
                IsOwner = owner,
                ProtectedAuthenticator = Protector(user.Id).Protect(secret),
                LastAcceptedStep = step
            });
            await database.SaveChangesAsync();
            await transaction.CommitAsync();
            return new PrivilegedAccount(user.Id, owner);
        }
        catch (DbUpdateException error) when (error.InnerException is SqlException { Number: 2601 or 2627 })
        {
            // The unique owner and username indexes close concurrent enrollment races.
            return null;
        }
    }

    public async Task<PrivilegedAccount?> AuthenticateAsync(CustomerEmail email, string password, string code)
    {
        var user = await users.FindByNameAsync(email.Value);
        if (user is null)
        {
            unknownPassword.Verify(password);
            return null;
        }

        var enrollment = await database.PrivilegedEnrollments.AsNoTracking()
            .SingleOrDefaultAsync(account => account.UserId == user.Id);
        if (enrollment is null)
        {
            unknownPassword.Verify(password);
            return null;
        }

        if (await users.IsLockedOutAsync(user))
        {
            return null;
        }

        if (!await users.CheckPasswordAsync(user, password) ||
            !TryVerify(Protector(user.Id).Unprotect(enrollment.ProtectedAuthenticator), code, out var step) ||
            step <= enrollment.LastAcceptedStep)
        {
            await users.AccessFailedAsync(user);
            return null;
        }

        // Claim this time step once, even across multiple Identity processes.
        var claimed = await database.PrivilegedEnrollments
            .Where(account => account.UserId == user.Id && account.LastAcceptedStep < step)
            .ExecuteUpdateAsync(update => update.SetProperty(account => account.LastAcceptedStep, step));

        return claimed == 1 && (await users.ResetAccessFailedCountAsync(user)).Succeeded
            ? new PrivilegedAccount(user.Id, enrollment.IsOwner)
            : null;
    }

    private bool TryVerify(string secret, string code, out long step) =>
        new Totp(Base32Encoding.ToBytes(secret))
            .VerifyTotp(clock.GetUtcNow().UtcDateTime, code, out step, new VerificationWindow(previous: 1));

    private IDataProtector Protector(Guid userId) =>
        protection.CreateProtector("FindYourFood.Identity.PrivilegedAuthenticator.v1", userId.ToString("D"));
}
