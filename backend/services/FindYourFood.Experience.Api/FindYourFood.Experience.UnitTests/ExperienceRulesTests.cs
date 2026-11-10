using FindYourFood.Experience.Domain;
using Xunit;

namespace FindYourFood.Experience.UnitTests;

public sealed class ExperienceRulesTests
{
    private static readonly ExperienceDocument Valid = new(1, "FindYourFood", "forest", "editorial", true,
        [new("account", "My account")], [new("hero", "Good food", "Find a favorite.")]);

    [Fact]
    public void Requires_supported_schema_and_known_theme_tokens()
    {
        Assert.True(ExperienceRules.IsValid(Valid));
        Assert.False(ExperienceRules.IsValid(Valid with { SchemaVersion = 2 }));
        Assert.False(ExperienceRules.IsValid(Valid with { Palette = "url(https://example.test/asset)" }));
        Assert.False(ExperienceRules.IsValid(Valid with { Typography = "custom-font" }));
        Assert.False(ExperienceRules.IsValid(Valid with { Brand = new string('x', 41) }));
    }

    [Fact]
    public void Requires_registered_components_and_safe_navigation_targets()
    {
        Assert.False(ExperienceRules.IsValid(Valid with { Sections = [new("script", "Title", "Body")] }));
        Assert.False(ExperienceRules.IsValid(Valid with { Sections = [.. Valid.Sections, .. Valid.Sections] }));
        Assert.False(ExperienceRules.IsValid(Valid with { Navigation = [new("javascript:alert(1)", "Link")] }));
        Assert.True(ExperienceRules.IsValid(Valid with { Sections = [new("banner", "Banner", "Text"), .. Valid.Sections] }));
    }

    [Fact]
    public void Restoring_a_publication_creates_a_new_version()
    {
        var state = new ExperienceWorkspace();
        var actor = Guid.NewGuid();
        state.SaveDraft("original");
        state.Approve(actor);
        var first = state.Publish(actor, DateTimeOffset.UtcNow);
        state.SaveDraft("updated");
        state.Approve(actor);
        var second = state.Publish(actor, DateTimeOffset.UtcNow);
        var restored = state.Restore(first, actor, DateTimeOffset.UtcNow);

        Assert.Equal(2, second.Version);
        Assert.Equal(3, restored.Version);
        Assert.Equal(first.Version, restored.RestoredFromVersion);
        Assert.Equal(first.ConfigurationJson, restored.ConfigurationJson);
        Assert.Equal("updated", state.DraftJson);
    }
}
