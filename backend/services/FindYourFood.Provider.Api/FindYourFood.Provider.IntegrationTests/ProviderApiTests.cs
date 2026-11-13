using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using FindYourFood.Provider.Application.DTOs;
using FindYourFood.Provider.Domain;
using Xunit;

namespace FindYourFood.Provider.IntegrationTests;

public sealed class ProviderApiTests(ProviderSqlFixture fixture) : IClassFixture<ProviderSqlFixture>
{
    private const string Me = "/api/v1/providers/me";
    private const string Admin = "/api/v1/providers/admin";
    private static readonly ProviderDetails Details = new("Example Kitchen", "cloud-kitchen", "owner@example.test", "Fresh meals.");

    [Fact]
    public async Task Provider_endpoints_require_the_expected_identity_and_fail_closed()
    {
        using var anonymous = fixture.Client();
        using var applicant = fixture.Client("applicant");
        using var outage = fixture.Client("outage");
        Assert.Equal(HttpStatusCode.OK, (await anonymous.GetAsync("/health/ready")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(Me)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await applicant.GetAsync(Admin)).StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await outage.GetAsync(Admin)).StatusCode);
    }

    [Fact]
    public async Task Application_review_documents_and_branch_changes_are_scoped_and_revisioned()
    {
        var subject = Guid.NewGuid();
        using var applicant = fixture.Client("applicant", subject);
        using var other = fixture.Client("applicant");
        using var reviewer = fixture.Client("reviewer");
        using var selfReviewer = fixture.Client("reviewer", subject);

        var profile = await Read(await applicant.PostAsJsonAsync(Me, Details), HttpStatusCode.Created);
        Assert.Equal("draft", profile.Status);
        Assert.Equal(HttpStatusCode.Conflict, (await applicant.PostAsJsonAsync(Me, Details)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync(Me)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await applicant.PostAsJsonAsync(Me + "/submit", new SubmitRequest(profile.Revision))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Upload(applicant, "license", profile.Revision, "<svg/>"u8.ToArray())).StatusCode);

        var pdf = "%PDF-1.7 sample"u8.ToArray();
        var stale = profile.Revision;
        profile = await Read(await Upload(applicant, "license", profile.Revision, pdf), HttpStatusCode.OK);
        Assert.Equal(HttpStatusCode.NotFound,
            (await reviewer.GetAsync(Admin + "/" + profile.Id + "/documents/license")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Upload(applicant, "address-proof", stale, pdf)).StatusCode);
        profile = await Read(await Upload(applicant, "address-proof", profile.Revision, pdf), HttpStatusCode.OK);
        profile = await Read(await applicant.PostAsJsonAsync(Me + "/submit", new SubmitRequest(profile.Revision)), HttpStatusCode.OK);

        var route = Admin + "/" + profile.Id;
        using var scopedReviewer = fixture.Client(profile.Id.ToString("N"));
        Assert.Equal(HttpStatusCode.Forbidden, (await scopedReviewer.GetAsync(Admin)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await scopedReviewer.GetAsync(route)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync(Me + "/documents/license")).StatusCode);
        using var file = await scopedReviewer.GetAsync(route + "/documents/license");
        Assert.Equal("attachment", file.Content.Headers.ContentDisposition!.DispositionType);
        Assert.Equal(pdf, await file.Content.ReadAsByteArrayAsync());
        Assert.Equal(HttpStatusCode.Conflict,
            (await selfReviewer.PostAsJsonAsync(route + "/review", new ReviewRequest(profile.Revision, true, null))).StatusCode);

        profile = await Read(await reviewer.PostAsJsonAsync(route + "/review",
            new ReviewRequest(profile.Revision, false, "Provide a clear license.")), HttpStatusCode.OK);
        Assert.Equal("rejected", profile.Status);
        profile = await Read(await applicant.PostAsJsonAsync(Me + "/submit", new SubmitRequest(profile.Revision)), HttpStatusCode.OK);
        profile = await Read(await scopedReviewer.PostAsJsonAsync(route + "/review",
            new ReviewRequest(profile.Revision, true, null)), HttpStatusCode.OK);
        Assert.Equal("approved", profile.Status);
        Assert.Equal(2, profile.Reviews.Count);
        Assert.Equal(HttpStatusCode.Conflict, (await applicant.PutAsJsonAsync(Me,
            new UpdateProviderRequest(profile.Revision, Details with { Name = "Changed name" }))).StatusCode);
        profile = await Read(await applicant.PutAsJsonAsync(Me,
            new UpdateProviderRequest(profile.Revision, Details with { Description = "Updated description." })), HttpStatusCode.OK);
        Assert.Equal("Updated description.", profile.Details.Description);

        var branch = new BranchDetails("Central", "42 Main Road", "Pune", "Asia/Kolkata",
            [new(1, "09:00", "17:00")], null, 18.52, 73.85);
        profile = await Read(await applicant.PostAsJsonAsync(Me + "/branches",
            new UpdateBranchRequest(profile.Revision, branch)), HttpStatusCode.Created);
        var branchId = Assert.Single(profile.Branches).Id;
        using var publicClient = fixture.Client();
        var published = (await publicClient.GetFromJsonAsync<PublicBranchView>(
            "/api/v1/providers/public/branches/" + branchId))!;
        Assert.Equal(branchId, published.Id);
        Assert.Equal(18.52, published.Details.Latitude);
        var directory = (await publicClient.GetFromJsonAsync<PublicBranchView[]>(
            "/api/v1/providers/public/branches?city=pune&page=1"))!;
        Assert.Contains(directory, item => item.Id == branchId);
        Assert.Equal(HttpStatusCode.NoContent,
            (await applicant.GetAsync(Me + "/branches/" + branchId + "/access")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await other.GetAsync(Me + "/branches/" + branchId + "/access")).StatusCode);
        var closure = DateTimeOffset.UtcNow.AddDays(1);
        profile = await Read(await applicant.PutAsJsonAsync(Me + "/branches/" + branchId,
            new UpdateBranchRequest(profile.Revision, branch with { ClosedUntil = closure })), HttpStatusCode.OK);
        Assert.Equal(closure, Assert.Single(profile.Branches).Details.ClosedUntil);
    }

    private static async Task<HttpResponseMessage> Upload(HttpClient client, string kind, int revision, byte[] content)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(revision.ToString(CultureInfo.InvariantCulture)), "expectedRevision");
        form.Add(new ByteArrayContent(content), "document", "document.pdf");
        return await client.PutAsync(Me + "/documents/" + kind, form);
    }

    private static async Task<ProviderView> Read(HttpResponseMessage response, HttpStatusCode expected)
    {
        using (response)
        {
            Assert.Equal(expected, response.StatusCode);
            return (await response.Content.ReadFromJsonAsync<ProviderView>())!;
        }
    }
}
