namespace FindYourFood.Customer.Domain;

public sealed record DeliveryAddress(Guid Id, string Label, string Street, string City, string PostalCode,
    double? Latitude, double? Longitude);

public sealed record ProfileSettings(string DisplayName, IReadOnlyList<DeliveryAddress> Addresses,
    Guid? PrimaryAddress, IReadOnlyList<string> DietaryPreferences, IReadOnlyList<Guid> FavoriteBranches,
    bool PersonalizedRecommendations)
{
    public static ProfileSettings Empty { get; } = new("", [], null, [], [], false);

    public bool IsValid() =>
        Text(DisplayName, 0, 80) && Addresses is { Count: <= 8 } && Addresses.All(ValidAddress) &&
        Addresses.Select(address => address.Id).Distinct().Count() == Addresses.Count &&
        (PrimaryAddress is null || Addresses.Any(address => address.Id == PrimaryAddress)) &&
        DietaryPreferences is { Count: <= 16 } && DietaryPreferences.All(item => Text(item, 1, 40)) &&
        DietaryPreferences.Distinct(StringComparer.OrdinalIgnoreCase).Count() == DietaryPreferences.Count &&
        FavoriteBranches is { Count: <= 60 } && FavoriteBranches.All(id => id != Guid.Empty) &&
        FavoriteBranches.Distinct().Count() == FavoriteBranches.Count;

    private static bool ValidAddress(DeliveryAddress? address) =>
        address is not null && address.Id != Guid.Empty && Text(address.Label, 1, 30) &&
        Text(address.Street, 1, 220) && Text(address.City, 1, 100) && Text(address.PostalCode, 3, 12) &&
        ((address.Latitude is null && address.Longitude is null) ||
            (address.Latitude is >= -90 and <= 90 && address.Longitude is >= -180 and <= 180));

    private static bool Text(string? value, int min, int max) => value is not null &&
        value.Length >= min && value.Length <= max && value == value.Trim() && !value.Any(char.IsControl);
}
