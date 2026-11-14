using System.Security.Cryptography;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FindYourFood.Messaging;

public interface IMessageStore
{
    Task<OutboxRecord?> ClaimAsync(Guid lease, DateTimeOffset now, CancellationToken ct);
    Task CompleteAsync(Guid id, Guid lease, DateTimeOffset now, CancellationToken ct);
    Task RetryAsync(OutboxRecord message, Guid lease, DateTimeOffset now, string reason, bool permanent, CancellationToken ct);
    Task RejectAsync(ReadOnlyMemory<byte> body, string reason, DateTimeOffset now, CancellationToken ct);
}

public sealed class SqlMessageStore(DbContext database) : IMessageStore
{
    public async Task<OutboxRecord?> ClaimAsync(Guid lease, DateTimeOffset now, CancellationToken ct)
    {
        var rows = database.Set<OutboxRecord>();
        var candidates = await rows.AsNoTracking()
            .Where(row => row.PublishedAt == null && row.FailedAt == null &&
                (row.NextAttemptAt == null || row.NextAttemptAt <= now) &&
                (row.LeaseUntil == null || row.LeaseUntil <= now))
            .OrderBy(row => row.OccurredAt).ThenBy(row => row.AggregateVersion).ThenBy(row => row.Id)
            .Select(row => row.Id).Take(8).ToListAsync(ct);
        foreach (var id in candidates)
        {
            var claimed = await rows.Where(row => row.Id == id && row.PublishedAt == null && row.FailedAt == null &&
                    (row.NextAttemptAt == null || row.NextAttemptAt <= now) &&
                    (row.LeaseUntil == null || row.LeaseUntil <= now))
                .ExecuteUpdateAsync(update => update.SetProperty(row => row.LeaseOwner, lease)
                    .SetProperty(row => row.LeaseUntil, now.AddSeconds(30))
                    .SetProperty(row => row.Attempts, row => row.Attempts + 1), ct);
            if (claimed == 1) return await rows.AsNoTracking().SingleAsync(row => row.Id == id, ct);
        }
        return null;
    }

    public async Task CompleteAsync(Guid id, Guid lease, DateTimeOffset now, CancellationToken ct) =>
        await database.Set<OutboxRecord>().Where(row => row.Id == id && row.LeaseOwner == lease && row.PublishedAt == null)
            .ExecuteUpdateAsync(update => update.SetProperty(row => row.PublishedAt, now)
                .SetProperty(row => row.LeaseOwner, (Guid?)null)
                .SetProperty(row => row.LeaseUntil, (DateTimeOffset?)null)
                .SetProperty(row => row.LastError, (string?)null), ct);

    public async Task RetryAsync(OutboxRecord message, Guid lease, DateTimeOffset now, string reason, bool permanent, CancellationToken ct)
    {
        var retryAt = now.AddSeconds(Math.Min(60, Math.Pow(2, Math.Min(message.Attempts, 6))));
        await database.Set<OutboxRecord>().Where(row => row.Id == message.Id && row.LeaseOwner == lease && row.PublishedAt == null)
            .ExecuteUpdateAsync(update => update.SetProperty(row => row.NextAttemptAt, retryAt)
                .SetProperty(row => row.FailedAt, permanent ? now : (DateTimeOffset?)null)
                .SetProperty(row => row.LastError, reason)
                .SetProperty(row => row.LeaseOwner, (Guid?)null)
                .SetProperty(row => row.LeaseUntil, (DateTimeOffset?)null), ct);
    }

    public async Task RejectAsync(ReadOnlyMemory<byte> body, string reason, DateTimeOffset now, CancellationToken ct)
    {
        var hash = Convert.ToHexString(SHA256.HashData(body.Span));
        database.Set<RejectedMessage>().Add(new(hash, Convert.ToBase64String(body.Span[..Math.Min(body.Length, 65536)]), reason, now));
        try { await database.SaveChangesAsync(ct); }
        catch (DbUpdateException error) when (error.GetBaseException() is SqlException { Number: 2601 or 2627 })
        {
            database.ChangeTracker.Clear();
        }
    }
}
