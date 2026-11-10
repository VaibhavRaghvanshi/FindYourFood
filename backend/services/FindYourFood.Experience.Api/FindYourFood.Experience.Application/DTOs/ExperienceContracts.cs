using FindYourFood.Experience.Domain;

namespace FindYourFood.Experience.Application.DTOs;

public sealed record SaveDraftRequest(int ExpectedRevision, ExperienceDocument? Configuration);
public sealed record PreviewRequest(ExperienceDocument? Configuration);
public sealed record RevisionRequest(int ExpectedRevision);
public sealed record RollbackRequest(int ExpectedRevision, int Version);

public sealed record PublishedExperience(int Version, ExperienceDocument Configuration);
public sealed record PublicationSummary(int Version, DateTimeOffset PublishedAt, Guid PublishedBy,
    Guid? ApprovedBy, int? RestoredFromVersion);
public sealed record WorkspaceView(int Revision, ExperienceDocument? Draft, string DraftStatus,
    int? PublishedVersion, IReadOnlyList<PublicationSummary> History);

public enum WorkflowStatus { Accepted, Invalid, Conflict, Missing }

public sealed record WorkflowResult(WorkflowStatus Status, WorkspaceView? Workspace = null);
