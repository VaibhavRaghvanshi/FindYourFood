using FindYourFood.Customer.Domain;

namespace FindYourFood.Customer.Application.DTOs;

public sealed record CustomerSnapshot(long Revision, ProfileSettings Settings);
public enum ProfileSaveStatus { Saved, Invalid, Conflict }
public sealed record ProfileSaveResult(ProfileSaveStatus Status, CustomerSnapshot? Profile = null);
