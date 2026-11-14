using FindYourFood.Contracts;
using FindYourFood.Messaging;
using FindYourFood.Payment.Application.Interfaces;
using FindYourFood.Payment.Domain;
using FindYourFood.Payment.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FindYourFood.Payment.Infrastructure.Repositories;

internal sealed class PaymentRepository(PaymentDbContext database) : IPaymentRepository
{
    public Task<PaymentIntent?> OrderAsync(Guid order, CancellationToken ct) =>
        database.Intents.SingleOrDefaultAsync(row => row.OrderId == order, ct);
    public Task<PaymentIntent?> IntentAsync(Guid id, CancellationToken ct) =>
        database.Intents.SingleOrDefaultAsync(row => row.Id == id, ct);
    public Task<PaymentAttempt?> AttemptAsync(Guid payment, CancellationToken ct) =>
        database.Set<PaymentAttempt>().SingleOrDefaultAsync(row => row.Id == payment, ct);
    public Task<PaymentAttempt?> ByKeyAsync(Guid intent, Guid key, CancellationToken ct) =>
        database.Set<PaymentAttempt>().SingleOrDefaultAsync(row => row.IntentId == intent && row.IdempotencyKey == key, ct);
    public Task<bool> WasCancelledAsync(Guid order, CancellationToken ct) =>
        database.Set<CancelledOrder>().AnyAsync(row => row.OrderId == order, ct);
    public Task<RefundInstruction?> RefundAsync(Guid payment, CancellationToken ct) =>
        database.Set<RefundInstruction>().SingleOrDefaultAsync(row => row.PaymentId == payment, ct);
    public async Task<IReadOnlyList<Guid>> PendingAttemptsAsync(DateTimeOffset now, CancellationToken ct) =>
        await database.Set<PaymentAttempt>().AsNoTracking()
            .Where(row => row.State == AttemptState.Pending && (row.NextRetryAt == null || row.NextRetryAt <= now))
            .OrderBy(row => row.CreatedAt).ThenBy(row => row.Id).Select(row => row.Id).Take(20).ToListAsync(ct);
    public async Task<IReadOnlyList<Guid>> PendingRefundsAsync(DateTimeOffset now, CancellationToken ct) =>
        await database.Set<RefundInstruction>().AsNoTracking()
            .Where(row => !row.Completed && (row.NextRetryAt == null || row.NextRetryAt <= now))
            .OrderBy(row => row.Id).Select(row => row.PaymentId).Take(20).ToListAsync(ct);
    public Task<string?> InboxHashAsync(Guid id, CancellationToken ct) =>
        database.Set<InboxRecord>().AsNoTracking().Where(row => row.MessageId == id)
            .Select(row => row.ContentHash).SingleOrDefaultAsync(ct);
    public void Add(PaymentIntent intent) => database.Intents.Add(intent);
    public void Add(PaymentAttempt attempt) => database.Set<PaymentAttempt>().Add(attempt);
    public void Add(RefundInstruction refund) => database.Set<RefundInstruction>().Add(refund);
    public void Add(CancelledOrder cancellation) => database.Set<CancelledOrder>().Add(cancellation);
    public void Add(InboxReceipt receipt) => database.Set<InboxRecord>().Add(InboxRecord.From(receipt));
    public void Add(IntegrationEvent message) => database.Set<OutboxRecord>().Add(new(message.MessageId,
        message.Source, message.Type, message.AggregateId, message.AggregateVersion,
        message.Data.GetRawText(), message.OccurredAt));
}
