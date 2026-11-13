using FindYourFood.Location.Domain;

namespace FindYourFood.Location.Application.DTOs;

public sealed record ZoneSnapshot(Guid Id, long Revision, ZoneSettings Settings);
public sealed record ZoneUpdate(long Revision, ZoneSettings Settings);
public sealed record DeliveryQuoteRequest(Coordinates Origin, Coordinates Destination);
public sealed record Serviceability(bool Serviceable, Guid? ZoneId = null, string? Zone = null, string? City = null,
    int? DeliveryFeeMinor = null, double? DistanceKm = null, string? EstimateMethod = null);
public enum ZoneSaveStatus { Saved, Invalid, Conflict }
public sealed record ZoneSaveResult(ZoneSaveStatus Status, ZoneSnapshot? Zone = null);
