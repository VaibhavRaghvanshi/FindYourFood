namespace FindYourFood.Catalog.Domain;

public static class MenuRules
{
    public static bool IsValid(MenuContent? menu)
    {
        if (menu is null || !Text(menu.Name, 80) || menu.Items is not { Count: > 0 and <= 24 })
        {
            return false;
        }

        var ids = new HashSet<Guid>();
        foreach (var item in menu.Items)
        {
            if (item is null || item.Id == Guid.Empty || !ids.Add(item.Id) ||
                !Text(item.Category, 40) || !Text(item.Name, 80) || !Text(item.Description, 180) ||
                item.PriceMinor is < 1 or > 2_000_000 ||
                !TextList(item.Ingredients) || !TextList(item.Tags) || !TextList(item.Allergens) ||
                !Options(item.Variants) || !Options(item.AddOns))
            {
                return false;
            }
        }

        return true;
    }

    private static bool Options(IReadOnlyList<MenuOption>? options)
    {
        if (options is not { Count: <= 5 }) return false;
        var ids = new HashSet<Guid>();
        return options.All(option => option is not null && option.Id != Guid.Empty && ids.Add(option.Id) &&
            Text(option.Name, 40) && option.ExtraMinor is >= 0 and <= 2_000_000);
    }

    private static bool TextList(IReadOnlyList<string>? values) =>
        values is { Count: <= 8 } && values.All(value => Text(value, 40)) &&
        values.Distinct(StringComparer.OrdinalIgnoreCase).Count() == values.Count;

    private static bool Text(string? value, int limit) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= limit && !value.Any(char.IsControl);
}
