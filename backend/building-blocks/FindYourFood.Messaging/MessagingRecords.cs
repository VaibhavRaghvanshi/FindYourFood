using FindYourFood.Contracts;
using Microsoft.EntityFrameworkCore;

namespace FindYourFood.Messaging;

public sealed class OutboxRecord(Guid id, string source, string type, Guid aggregateId, long aggregateVersion,
    string payloadJson, DateTimeOffset occurredAt)
{
    public Guid Id { get; private set; } = id;
    public string Source { get; private set; } = source;
    public string Type { get; private set; } = type;
    public Guid AggregateId { get; private set; } = aggregateId;
    public long AggregateVersion { get; private set; } = aggregateVersion;
    public string PayloadJson { get; private set; } = payloadJson;
    public DateTimeOffset OccurredAt { get; private set; } = occurredAt;
    public DateTimeOffset? PublishedAt { get; private set; }
    public Guid? LeaseOwner { get; private set; }
    public DateTimeOffset? LeaseUntil { get; private set; }
    public DateTimeOffset? NextAttemptAt { get; private set; }
    public DateTimeOffset? FailedAt { get; private set; }
    public int Attempts { get; private set; }
    public string? LastError { get; private set; }
}

public sealed class InboxRecord(Guid messageId, string source, string type, string contentHash, DateTimeOffset processedAt)
{
    public Guid MessageId { get; private set; } = messageId;
    public string Source { get; private set; } = source;
    public string Type { get; private set; } = type;
    public string ContentHash { get; private set; } = contentHash;
    public DateTimeOffset ProcessedAt { get; private set; } = processedAt;

    public static InboxRecord From(InboxReceipt receipt) =>
        new(receipt.MessageId, receipt.Source, receipt.Type, receipt.ContentHash, receipt.ProcessedAt);
}

public sealed class RejectedMessage(string contentHash, string bodyBase64, string reason, DateTimeOffset receivedAt)
{
    public string ContentHash { get; private set; } = contentHash;
    public string BodyBase64 { get; private set; } = bodyBase64;
    public string Reason { get; private set; } = reason;
    public DateTimeOffset ReceivedAt { get; private set; } = receivedAt;
}

public static class MessagingModel
{
    public static void Configure(ModelBuilder model)
    {
        var outgoing = model.Entity<OutboxRecord>();
        outgoing.ToTable("Outbox");
        outgoing.HasKey(row => row.Id);
        outgoing.Property(row => row.Id).ValueGeneratedNever();
        outgoing.Property(row => row.AggregateId).HasColumnName("OrderId");
        outgoing.Property(row => row.Source).HasMaxLength(32).HasDefaultValue("");
        outgoing.Property(row => row.Type).HasMaxLength(100);
        outgoing.Property(row => row.AggregateVersion).HasDefaultValue(1L);
        outgoing.Property(row => row.LastError).HasMaxLength(100);
        outgoing.HasIndex(row => new { row.PublishedAt, row.OccurredAt });
        outgoing.HasIndex(row => new { row.FailedAt, row.NextAttemptAt, row.LeaseUntil });

        var incoming = model.Entity<InboxRecord>();
        incoming.ToTable("Inbox");
        incoming.HasKey(row => row.MessageId);
        incoming.Property(row => row.MessageId).ValueGeneratedNever();
        incoming.Property(row => row.Source).HasMaxLength(32);
        incoming.Property(row => row.Type).HasMaxLength(100);
        incoming.Property(row => row.ContentHash).HasMaxLength(64).IsUnicode(false);

        var rejected = model.Entity<RejectedMessage>();
        rejected.ToTable("RejectedMessages");
        rejected.HasKey(row => row.ContentHash);
        rejected.Property(row => row.ContentHash).HasMaxLength(64).IsUnicode(false);
        rejected.Property(row => row.Reason).HasMaxLength(100);
        rejected.Property(row => row.BodyBase64).HasMaxLength(90000).IsUnicode(false);
    }
}
