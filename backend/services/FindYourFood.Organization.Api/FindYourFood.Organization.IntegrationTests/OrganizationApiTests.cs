using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FindYourFood.Organization.Application;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FindYourFood.Organization.IntegrationTests;

public sealed class OrganizationApiTests(OrganizationSqlFixture fixture) : IClassFixture<OrganizationSqlFixture>
{
    private const string Root = "/api/v1/organization";

    [Fact]
    public async Task Service_checks_sql_readiness_and_requires_staff_mfa()
    {
        using var anonymous = fixture.Client();
        using var customer = fixture.Client(Guid.NewGuid(), mfa: false);
        using var incompleteStaff = fixture.Client(Guid.NewGuid(), "staff", mfa: false);

        Assert.Equal(HttpStatusCode.OK, (await anonymous.GetAsync("/health/ready")).StatusCode);
        var openApi = await anonymous.GetFromJsonAsync<JsonElement>("/openapi/v1.json");
        Assert.True(openApi.GetProperty("paths").TryGetProperty(Root + "/permissions/check", out _));
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(Root + "/grants")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await customer.GetAsync(Root + "/grants")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await incompleteStaff.GetAsync(Root + "/grants")).StatusCode);
    }

    [Fact]
    public async Task Scoped_delegation_rejects_escalation_and_parent_revocation_invalidates_descendants()
    {
        var ownerId = Guid.NewGuid();
        var managerId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        using var owner = fixture.Client(ownerId, "owner");
        using var manager = fixture.Client(managerId, "staff");
        using var employee = fixture.Client(employeeId, "staff");
        using var unrelated = fixture.Client(Guid.NewGuid(), "staff");

        Assert.True(await Allowed(owner, "/city/pune"));
        using var numericLevel = await owner.PostAsJsonAsync(Root + "/grants", new
        {
            subjectId = managerId, level = 3, scope = "/city/pune", permission = "provider.review", canDelegate = true
        });
        Assert.Equal(HttpStatusCode.BadRequest, numericLevel.StatusCode);
        var root = await Create(owner, managerId, "CityManager", "/city/pune", true);
        using var duplicate = await Grant(owner, managerId, "CityManager", "/city/pune", true);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        Assert.Equal(HttpStatusCode.Forbidden,
            (await Grant(manager, employeeId, "Employee", "/city/pune-east", false, root)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await Grant(manager, employeeId, "Ceo", "/city/pune", false, root)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await Grant(manager, managerId, "Employee", "/city/pune", false, root)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await Grant(manager, employeeId, "Employee", "/city/pune", false, root, "finance.report.read")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await Grant(unrelated, employeeId, "Employee", "/city/pune", false, root)).StatusCode);

        var child = await Create(manager, employeeId, "Employee", "/city/pune/zone-a", false, root);
        Assert.True(await Allowed(employee, "/city/pune/zone-a/restaurant-4"));
        Assert.False(await Allowed(employee, "/city/pune/zone-b"));
        Assert.Equal(HttpStatusCode.Forbidden, (await unrelated.DeleteAsync(Root + $"/grants/{child}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await owner.DeleteAsync(Root + $"/grants/{root}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await owner.DeleteAsync(Root + $"/grants/{root}")).StatusCode);
        Assert.False(await Allowed(employee, "/city/pune/zone-a"));
        Assert.Equal(HttpStatusCode.Forbidden,
            (await Grant(manager, Guid.NewGuid(), "Employee", "/city/pune", false, root)).StatusCode);

        await using var db = fixture.CreateDatabase();
        var audit = await db.Set<GrantAudit>().Where(entry => entry.GrantId == root || entry.GrantId == child).ToListAsync();
        Assert.Equal(3, audit.Count);
        Assert.Contains(audit, entry => entry.ActorId == ownerId && entry.Action == "revoked" && entry.TraceId.Length > 0);
        Assert.Contains(audit, entry => entry.ActorId == managerId && entry.GrantId == child && entry.Action == "granted");
    }

    private static async Task<Guid> Create(
        HttpClient client, Guid subject, string level, string scope, bool canDelegate, Guid? parent = null)
    {
        using var response = await Grant(client, subject, level, scope, canDelegate, parent);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private static Task<HttpResponseMessage> Grant(
        HttpClient client, Guid subject, string level, string scope, bool canDelegate,
        Guid? parent = null, string permission = "provider.review") =>
        client.PostAsJsonAsync(Root + "/grants", new
        {
            subjectId = subject, level, scope, permission, canDelegate, parentGrantId = parent
        });

    private static async Task<bool> Allowed(HttpClient client, string scope) =>
        (await client.GetFromJsonAsync<JsonElement>($"{Root}/permissions/check?permission=provider.review&scope={Uri.EscapeDataString(scope)}"))
            .GetProperty("allowed").GetBoolean();
}
