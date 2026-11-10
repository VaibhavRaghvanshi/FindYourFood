using FindYourFood.Customer.Domain;
using Xunit;

namespace FindYourFood.Customer.UnitTests;

public sealed class ProfileSettingsTests
{
    [Fact]
    public void RequiresOwnedPrimaryAddressAndPairedCoordinates()
    {
        var id = Guid.NewGuid();
        var address = new DeliveryAddress(id, "Home", "Example street", "Pune", "411001", 18.52, 73.85);
        var profile = ProfileSettings.Empty with { Addresses = [address], PrimaryAddress = id };
        Assert.True(profile.IsValid());
        Assert.False((profile with { PrimaryAddress = Guid.NewGuid() }).IsValid());
        Assert.False((profile with { Addresses = [address with { Longitude = null }] }).IsValid());
    }
}
