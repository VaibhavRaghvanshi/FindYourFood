namespace FindYourFood.Cart.Domain;

public sealed record CartSelection(Guid LineId, Guid ItemId, int Quantity, Guid? VariantId, IReadOnlyList<Guid> AddOnIds)
{
    public static bool Valid(IReadOnlyList<CartSelection>? lines) =>
        lines is { Count: <= 20 } && lines.All(line => line is not null && line.LineId != Guid.Empty &&
            line.ItemId != Guid.Empty && line.Quantity is >= 1 and <= 20 &&
            line.VariantId != Guid.Empty && line.AddOnIds is { Count: <= 5 } &&
            line.AddOnIds.All(id => id != Guid.Empty) &&
            line.AddOnIds.Distinct().Count() == line.AddOnIds.Count) &&
        lines.Select(line => line.LineId).Distinct().Count() == lines.Count;
}

public sealed record MenuOption(Guid Id, string Name, int ExtraMinor);
public sealed record MenuItem(Guid Id, string Name, int PriceMinor, bool Available,
    IReadOnlyList<MenuOption> Variants, IReadOnlyList<MenuOption> AddOns);
public sealed record MenuContent(string Name, IReadOnlyList<MenuItem> Items);
public sealed record PublishedMenu(Guid BranchId, long Version, string Currency, MenuContent Menu);
public sealed record PricedLine(Guid LineId, Guid ItemId, string Name, int Quantity, string? Variant,
    IReadOnlyList<string> AddOns, long UnitPriceMinor, long TotalMinor, string? Error);
public sealed record CartPrices(PricedLine[] Lines, long? SubtotalMinor);

public static class CartPricing
{
    public static CartPrices Calculate(PublishedMenu publication, IReadOnlyList<CartSelection> selections)
    {
        var items = publication.Menu.Items.ToDictionary(item => item.Id);
        var lines = selections.Select(selection => Price(selection, items.GetValueOrDefault(selection.ItemId))).ToArray();
        var subtotal = lines.Sum(line => line.TotalMinor);
        var valid = publication.Currency == "INR" && lines.All(line => line.Error is null) && subtotal <= 100_000_000;
        return new(lines, valid ? subtotal : null);
    }

    private static PricedLine Price(CartSelection selection, MenuItem? item)
    {
        var variant = item?.Variants.SingleOrDefault(option => option.Id == selection.VariantId);
        var addOns = item?.AddOns.Where(option => selection.AddOnIds.Contains(option.Id)).ToArray() ?? [];
        var invalid = item is null || !item.Available ||
            (selection.VariantId is not null && variant is null) ||
            (item.Variants.Count > 0 && variant is null) || addOns.Length != selection.AddOnIds.Count;
        var unit = invalid ? 0L : item!.PriceMinor + (long)(variant?.ExtraMinor ?? 0) + addOns.Sum(option => (long)option.ExtraMinor);
        return new(selection.LineId, selection.ItemId, item?.Name ?? "Item no longer available", selection.Quantity,
            variant?.Name, addOns.Select(option => option.Name).ToArray(), unit, unit * selection.Quantity,
            invalid ? "This item or option is unavailable. Remove it and choose again." : null);
    }
}
