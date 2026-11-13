using FindYourFood.Location.Domain;
using Xunit;

namespace FindYourFood.Location.UnitTests;

public sealed class CoordinatesTests
{
    [Fact]
    public void DistanceUsesShortestPathAcrossTheAntimeridian()
    {
        Assert.InRange(new Coordinates(0, 179.9).DistanceTo(new(0, -179.9)), 22.23, 22.25);
        Assert.False(new Coordinates(91, 0).IsValid);
        Assert.False(new Coordinates(double.NaN, 0).IsValid);
    }
}
