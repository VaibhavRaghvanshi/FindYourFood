using FindYourFood.Discovery.Domain;
using Xunit;

namespace FindYourFood.Discovery.UnitTests;

public sealed class OpeningHoursTests
{
    [Fact]
    public void EvaluatesHoursInTheBranchTimeZoneAndHonorsTemporaryClosure()
    {
        var now = new DateTimeOffset(2026, 11, 9, 5, 0, 0, TimeSpan.Zero);
        var details = new PublicBranchDetails("Central", "Sample street", "Pune", "Asia/Kolkata",
            [new(1, "10:00", "12:00")], null, null, null);
        var branch = new PublicBranch(Guid.NewGuid(), "Kitchen", "restaurant", details);
        Assert.True(branch.IsOpen(now));
        Assert.False(branch.IsOpen(now.AddHours(2)));
        Assert.False((branch with { Details = details with { ClosedUntil = now.AddHours(1) } }).IsOpen(now));
    }
}
