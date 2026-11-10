namespace FindYourFood.Catalog.Domain;

public sealed record MenuContent(string Name, IReadOnlyList<MenuItem> Items);

public sealed record MenuItem(
    Guid Id, string Category, string Name, string Description, int PriceMinor, bool Available,
    IReadOnlyList<string> Ingredients, IReadOnlyList<string> Tags, IReadOnlyList<string> Allergens,
    IReadOnlyList<MenuOption> Variants, IReadOnlyList<MenuOption> AddOns);

public sealed record MenuOption(Guid Id, string Name, int ExtraMinor);
