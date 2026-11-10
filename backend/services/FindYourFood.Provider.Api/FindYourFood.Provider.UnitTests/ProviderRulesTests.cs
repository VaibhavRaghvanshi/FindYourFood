using FindYourFood.Provider.Domain;
using Xunit;

namespace FindYourFood.Provider.UnitTests;

public sealed class ProviderRulesTests
{
    [Fact]
    public void Uploads_require_allowed_signatures_and_stay_within_the_limit()
    {
        Assert.Equal("application/pdf", ProviderRules.DetectMediaType("%PDF-1.7"u8));
        Assert.Null(ProviderRules.DetectMediaType("<svg/>"u8));
        Assert.Null(ProviderRules.DetectMediaType(new byte[ProviderRules.MaxDocumentBytes + 1]));
    }

    [Fact]
    public void Branch_hours_validate_day_uniqueness_and_local_times()
    {
        var now = DateTimeOffset.UtcNow;
        var branch = new BranchDetails("Main", "A street", "Pune", "Asia/Kolkata",
            [new(1, "09:00", "18:00")], now.AddDays(1));
        Assert.True(ProviderRules.ValidBranch(branch, now));
        Assert.False(ProviderRules.ValidBranch(branch with { Hours = [new(1, "18:00", "09:00")] }, now));
        Assert.False(ProviderRules.ValidBranch(branch with { Hours = [.. branch.Hours, .. branch.Hours] }, now));
        Assert.False(ProviderRules.ValidBranch(branch with { TimeZone = "Unknown/Zone" }, now));
        Assert.False(ProviderRules.ValidBranch(branch with { ClosedUntil = now.AddDays(91) }, now));
    }

    [Fact]
    public void Rejections_require_a_note()
    {
        Assert.True(ProviderRules.ValidReview(true, null));
        Assert.False(ProviderRules.ValidReview(false, " "));
        Assert.True(ProviderRules.ValidReview(false, "Upload a clearer document."));
    }
}
