using FindYourFood.Provider.Domain;

namespace FindYourFood.Provider.Application.DTOs;

public sealed record UpdateProviderRequest(int ExpectedRevision, ProviderDetails Details);
public sealed record UpdateBranchRequest(int ExpectedRevision, BranchDetails Branch);
public sealed record SubmitRequest(int ExpectedRevision);
public sealed record ReviewRequest(int ExpectedRevision, bool Approved, string? Note);

public sealed record DocumentView(string Kind, string MediaType, int Size, DateTimeOffset UploadedAt);
public sealed record BranchView(Guid Id, BranchDetails Details);
public sealed record ProviderSummary(Guid Id, string Name, string Kind, DateTimeOffset UpdatedAt);
public sealed record ProviderView(Guid Id, int Revision, string Status, ProviderDetails Details, string? ReviewNote,
    IReadOnlyList<DocumentView> Documents, IReadOnlyList<BranchView> Branches, IReadOnlyList<ProviderReview> Reviews);

public enum ProviderResultStatus { Accepted, Invalid, Missing, Conflict }

public sealed record ProviderResult(ProviderResultStatus Status, ProviderView? Provider = null);
