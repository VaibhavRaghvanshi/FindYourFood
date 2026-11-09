using FindYourFood.Organization.Domain;

namespace FindYourFood.Organization.Application.DTOs;

public enum GrantStatus { Accepted, Invalid, Forbidden, Missing, Conflict }

public sealed record GrantResult(GrantStatus Status, AccessGrant? Grant = null);
