namespace FindYourFood.Ordering.Domain;

public sealed record DeliveryContact(string Name, string Phone, string Street, string City, string PostalCode,
    double Latitude, double Longitude)
{
    public bool IsValid() =>
        Text(Name, 80) && Text(Street, 220) && Text(City, 100) && Text(PostalCode, 12) &&
        PostalCode.Length >= 3 && Phone is { Length: >= 10 and <= 16 } && ValidPhone(Phone) &&
        Latitude is >= -90 and <= 90 && Longitude is >= -180 and <= 180;

    public DeliveryContact Normalized() =>
        this with { Name = Name.Trim(), Phone = Phone.Trim(), Street = Street.Trim(), City = City.Trim(), PostalCode = PostalCode.Trim() };

    private static bool Text(string? text, int maximum) =>
        !string.IsNullOrWhiteSpace(text) && text.Length <= maximum && !text.Any(char.IsControl);

    private static bool ValidPhone(string phone)
    {
        var digits = phone.StartsWith('+') ? phone[1..] : phone;
        return digits.Length is >= 10 and <= 15 && digits.All(character => character is >= '0' and <= '9');
    }
}

public sealed record OrderItem(Guid LineId, Guid ItemId, string Name, int Quantity, Guid? VariantId,
    string? VariantName, IReadOnlyList<Guid> AddOnIds, IReadOnlyList<string> AddOnNames, long UnitPriceMinor, long TotalMinor);
public sealed record OrderSnapshot(Guid BranchId, string MenuName, long MenuVersion, string Currency,
    IReadOnlyList<OrderItem> Items, long ItemsMinor, long DeliveryFeeMinor, long TotalMinor, Guid ZoneId,
    DeliveryContact Delivery, DateTimeOffset PriceCheckedAt)
{
    public bool IsValid() =>
        BranchId != Guid.Empty && ZoneId != Guid.Empty && MenuVersion > 0 && Currency == "INR" &&
        !string.IsNullOrWhiteSpace(MenuName) && MenuName.Length <= 80 && Delivery?.IsValid() == true &&
        Items is { Count: > 0 and <= 20 } && Items.All(item => item is not null && item.LineId != Guid.Empty &&
            item.ItemId != Guid.Empty && !string.IsNullOrWhiteSpace(item.Name) && item.Name.Length <= 80 &&
            item.Quantity is >= 1 and <= 20 && item.UnitPriceMinor is > 0 and <= 14_000_000 &&
            item.TotalMinor == item.UnitPriceMinor * item.Quantity &&
            item.AddOnIds is { Count: <= 5 } && item.AddOnNames is { Count: <= 5 } &&
            item.AddOnIds.Count == item.AddOnNames.Count) &&
        Items.Select(item => item.LineId).Distinct().Count() == Items.Count &&
        ItemsMinor is > 0 and <= 100_000_000 && Items.Sum(item => item.TotalMinor) == ItemsMinor &&
        DeliveryFeeMinor is >= 0 and <= 100_000 && TotalMinor == ItemsMinor + DeliveryFeeMinor;
}
