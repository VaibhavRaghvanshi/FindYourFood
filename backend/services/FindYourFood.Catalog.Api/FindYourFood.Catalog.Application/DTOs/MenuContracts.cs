using FindYourFood.Catalog.Domain;

namespace FindYourFood.Catalog.Application.DTOs;

public sealed record MenuDraft(int ExpectedRevision, MenuContent Menu);
public sealed record PublishMenu(int ExpectedRevision);
public sealed record MenuWorkspace(int Revision, int PublishedVersion, bool HasUnpublishedChanges, MenuContent? Draft);
public sealed record PublishedMenu(Guid BranchId, int Version, string Currency, MenuContent Menu);
public enum MenuStatus { Accepted, Invalid, Conflict }
public sealed record MenuResult(MenuStatus Status, MenuWorkspace? Workspace = null);
