namespace FindYourFood.Organization.Application;

public sealed record GrantAudit(
    Guid Id, Guid GrantId, Guid ActorId, string Action, string TraceId, DateTimeOffset OccurredAt);
