using FindYourFood.Organization.Domain;
using Xunit;

namespace FindYourFood.Organization.UnitTests;

public sealed class StaffAccessTests
{
    [Theory]
    [InlineData("/", "/city/pune", true)]
    [InlineData("/city/pune", "/city/pune/provider-4", true)]
    [InlineData("/city/pune", "/city/pune-east", false)]
    [InlineData("/city/pune", "/", false)]
    public void Scope_requires_whole_path_segments(string parent, string child, bool covers)
    {
        Assert.Equal(covers, StaffAccess.Covers(parent, child));
    }

    [Theory]
    [InlineData("/city/../provider")]
    [InlineData("/city//provider")]
    [InlineData("/city/Provider")]
    [InlineData("/city/pune/")]
    public void Scope_rejects_noncanonical_paths(string scope)
    {
        Assert.False(StaffAccess.IsScope(scope));
    }

    [Fact]
    public void Delegation_requires_an_active_grant_for_same_permission_and_lower_level()
    {
        var parent = new AccessGrant(Guid.NewGuid(), Guid.NewGuid(), StaffLevel.CityManager, "provider.review",
            "/city/pune", true, null, Guid.NewGuid(), DateTimeOffset.UtcNow);
        var request = new GrantRequest(Guid.NewGuid(), StaffLevel.TeamManager, "provider.review",
            "/city/pune/zone-a", false, parent.Id);

        Assert.True(StaffAccess.CanDelegate(parent, request));
        Assert.False(StaffAccess.CanDelegate(parent, request with { Level = StaffLevel.CityManager }));
        Assert.False(StaffAccess.CanDelegate(parent, request with { Permission = "finance.report.read" }));
        parent.Revoke(DateTimeOffset.UtcNow);
        Assert.False(StaffAccess.CanDelegate(parent, request));
    }
}
