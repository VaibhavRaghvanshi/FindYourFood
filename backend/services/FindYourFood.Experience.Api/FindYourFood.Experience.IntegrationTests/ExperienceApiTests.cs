using System.Net;
using System.Net.Http.Json;
using FindYourFood.Experience.Application.DTOs;
using FindYourFood.Experience.Domain;
using Xunit;

namespace FindYourFood.Experience.IntegrationTests;

public sealed class ExperienceApiTests(ExperienceSqlFixture fixture) : IClassFixture<ExperienceSqlFixture>
{
    private const string Public = "/api/v1/experience/customer-home";
    private const string Admin = "/api/v1/experience/admin/customer-home";

    [Fact]
    public async Task Workspace_is_protected_and_permission_outages_fail_closed()
    {
        using var anonymous = fixture.Client();
        using var customer = fixture.Client("customer");
        using var unassigned = fixture.Client("unassigned");
        using var outage = fixture.Client("outage");
        Assert.Equal(HttpStatusCode.OK, (await anonymous.GetAsync("/health/ready")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(Admin)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await customer.GetAsync(Admin)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await unassigned.GetAsync(Admin)).StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await outage.GetAsync(Admin)).StatusCode);
    }

    [Fact]
    public async Task Draft_preview_approval_publish_and_rollback_preserve_live_versions()
    {
        using var customer = fixture.Client();
        using var editor = fixture.Client("editor");
        using var publisher = fixture.Client("publisher");
        var first = Document("Original headline");
        var second = Document("New headline");

        Assert.Equal(HttpStatusCode.NoContent, (await customer.GetAsync(Public)).StatusCode);
        var workspace = (await editor.GetFromJsonAsync<WorkspaceView>(Admin))!;
        var revision = workspace.Revision;

        using var invalid = await editor.PutAsJsonAsync(Admin + "/draft",
            new SaveDraftRequest(revision, first with { Sections = [new("iframe", "Title", "Body")] }));
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);

        using var preview = await editor.PostAsJsonAsync(Admin + "/preview", new PreviewRequest(first));
        Assert.Equal(HttpStatusCode.OK, preview.StatusCode);
        Assert.Equal(revision, (await editor.GetFromJsonAsync<WorkspaceView>(Admin))!.Revision);
        Assert.Equal(HttpStatusCode.NoContent, (await customer.GetAsync(Public)).StatusCode);

        var writes = await Task.WhenAll(
            editor.PutAsJsonAsync(Admin + "/draft", new SaveDraftRequest(revision, first)),
            editor.PutAsJsonAsync(Admin + "/draft", new SaveDraftRequest(revision, first)));
        Assert.Single(writes, response => response.StatusCode == HttpStatusCode.OK);
        Assert.Single(writes, response => response.StatusCode == HttpStatusCode.Conflict);
        workspace = (await writes.Single(response => response.IsSuccessStatusCode).Content.ReadFromJsonAsync<WorkspaceView>())!;

        Assert.Equal(HttpStatusCode.Forbidden,
            (await editor.PostAsJsonAsync(Admin + "/approve", new RevisionRequest(workspace.Revision))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,
            (await publisher.PostAsJsonAsync(Admin + "/publish", new RevisionRequest(workspace.Revision))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await customer.GetAsync(Public)).StatusCode);

        workspace = await Transition(publisher, "approve", workspace.Revision);
        workspace = await Transition(publisher, "publish", workspace.Revision);
        Assert.Equal(1, workspace.PublishedVersion);
        await CheckLive(customer, 1, "Original headline");

        using var save = await editor.PutAsJsonAsync(Admin + "/draft", new SaveDraftRequest(workspace.Revision, second));
        Assert.Equal(HttpStatusCode.OK, save.StatusCode);
        workspace = (await save.Content.ReadFromJsonAsync<WorkspaceView>())!;
        workspace = await Transition(publisher, "approve", workspace.Revision);
        workspace = await Transition(publisher, "publish", workspace.Revision);
        await CheckLive(customer, 2, "New headline");

        using var rollback = await publisher.PostAsJsonAsync(Admin + "/rollback", new RollbackRequest(workspace.Revision, 1));
        Assert.Equal(HttpStatusCode.OK, rollback.StatusCode);
        workspace = (await rollback.Content.ReadFromJsonAsync<WorkspaceView>())!;
        Assert.Equal(new[] { 3, 2, 1 }, workspace.History.Select(release => release.Version));
        Assert.Equal(1, workspace.History[0].RestoredFromVersion);
        await CheckLive(customer, 3, "Original headline");
    }

    private static ExperienceDocument Document(string title) =>
        new(1, "FindYourFood", "forest", "editorial", true, [new("account", "My account")], [new("hero", title, "Text")]);

    private static async Task<WorkspaceView> Transition(HttpClient client, string action, int revision)
    {
        using var response = await client.PostAsJsonAsync(Admin + "/" + action, new RevisionRequest(revision));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<WorkspaceView>())!;
    }

    private static async Task CheckLive(HttpClient client, int version, string title)
    {
        var published = (await client.GetFromJsonAsync<PublishedExperience>(Public))!;
        Assert.Equal(version, published.Version);
        Assert.Equal(title, published.Configuration.Sections[0].Title);
    }
}
