using FindYourFood.Cart.Domain;
using Xunit;

namespace FindYourFood.Cart.UnitTests;

public sealed class CartPricingTests
{
    [Fact]
    public void PricesOnlyPublishedOptionsAndUsesWholePaise()
    {
        var item = new MenuItem(Guid.NewGuid(), "Soup", 12000, true,
            [new(Guid.NewGuid(), "Large", 2000)], [new(Guid.NewGuid(), "Bread", 300)]);
        var menu = new PublishedMenu(Guid.NewGuid(), 1, "INR", new("Lunch", [item]));
        var selection = new CartSelection(Guid.NewGuid(), item.Id, 2, item.Variants[0].Id, [item.AddOns[0].Id]);
        Assert.Equal(28600, CartPricing.Calculate(menu, [selection]).SubtotalMinor);
        Assert.Null(CartPricing.Calculate(menu, [selection with { AddOnIds = [Guid.NewGuid()] }]).SubtotalMinor);
        Assert.Null(CartPricing.Calculate(menu, [selection with { VariantId = null }]).SubtotalMinor);
        var unavailable = menu with { Menu = new("Lunch", [item with { Available = false }]) };
        Assert.Null(CartPricing.Calculate(unavailable, [selection]).SubtotalMinor);
    }
}
