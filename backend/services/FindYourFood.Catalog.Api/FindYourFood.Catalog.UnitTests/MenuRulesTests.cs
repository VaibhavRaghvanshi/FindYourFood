using FindYourFood.Catalog.Domain;
using Xunit;

namespace FindYourFood.Catalog.UnitTests;

public sealed class MenuRulesTests
{
    [Fact]
    public void Prices_and_item_ids_are_validated()
    {
        var item = new MenuItem(Guid.NewGuid(), "Meals", "Rice bowl", "Fresh.", 12500, true, [], [], [], [], []);
        Assert.True(MenuRules.IsValid(new MenuContent("Main menu", [item])));
        Assert.False(MenuRules.IsValid(new MenuContent("Main menu", [item, item])));
        Assert.False(MenuRules.IsValid(new MenuContent("Main menu", [item with { PriceMinor = 0 }])));
        Assert.False(MenuRules.IsValid(new MenuContent("Main menu", [item with { Allergens = ["milk", "Milk"] }])));
    }
}
