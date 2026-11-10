using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using FindYourFood.Catalog.Application.DTOs;
using FindYourFood.Catalog.Domain;
using FindYourFood.Catalog.Infrastructure.Data;
using FindYourFood.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FindYourFood.Catalog.IntegrationTests;

public sealed class CatalogApiTests(CatalogFixture fixture) : IClassFixture<CatalogFixture>
{
    [Fact]
    public async Task Only_the_approved_branch_owner_can_publish_a_draft()
    {
        var branch = Guid.NewGuid();
        var route = $"/api/v1/catalog/branches/{branch}";
        using var owner = fixture.Client(Guid.NewGuid(), new Claim("test_branch", branch.ToString("D")));
        using var other = fixture.Client(Guid.NewGuid());
        using var customer = fixture.Client();
        var item = new MenuItem(Guid.NewGuid(), "Meals", "Rice bowl", "Fresh.", 12900, true, ["Rice"],
            ["vegetarian"], [], [new(Guid.NewGuid(), "Large", 3000)], [new(Guid.NewGuid(), "Sauce", 1500)]);
        var draft = new MenuContent("Lunch", [item]);

        Assert.Equal(HttpStatusCode.Forbidden, (await other.PutAsJsonAsync(route + "/draft", new MenuDraft(0, draft))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await customer.GetAsync(route)).StatusCode);
        using var save = await owner.PutAsJsonAsync(route + "/draft", new MenuDraft(0, draft));
        Assert.Equal(HttpStatusCode.OK, save.StatusCode);
        var state = (await save.Content.ReadFromJsonAsync<MenuWorkspace>())!;
        Assert.Equal(HttpStatusCode.NotFound, (await customer.GetAsync(route)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await owner.PutAsJsonAsync(route + "/draft", new MenuDraft(0, draft))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await owner.PostAsJsonAsync(route + "/publish", new PublishMenu(state.Revision))).StatusCode);
        var release = (await customer.GetFromJsonAsync<PublishedMenu>(route))!;
        Assert.Equal(1, release.Version);
        Assert.Equal(12900, release.Menu.Items[0].PriceMinor);
        Assert.Equal("Sauce", release.Menu.Items[0].AddOns[0].Name);
        Assert.Equal(HttpStatusCode.OK, (await owner.PutAsJsonAsync(route + "/draft",
            new MenuDraft(state.Revision + 1, new MenuContent("New draft", [item])))).StatusCode);
        Assert.Equal("Lunch", (await customer.GetFromJsonAsync<PublishedMenu>(route))!.Menu.Name);
    }
}

public sealed class CatalogFixture() : SqlServiceFixture<CatalogHost, CatalogDbContext>("Catalog", options => new(options))
{
    protected override void Configure(IServiceCollection services) =>
        services.AddHttpClient("provider").ConfigurePrimaryHttpMessageHandler(() => new ProviderAccess());

    private sealed class ProviderAccess : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(request.Headers.Authorization!.Parameter);
            var branch = jwt.Claims.FirstOrDefault(claim => claim.Type == "test_branch")?.Value;
            var allowed = branch is not null && request.RequestUri!.AbsolutePath == $"/api/v1/providers/me/branches/{branch}/access";
            return Task.FromResult(new HttpResponseMessage(allowed ? HttpStatusCode.NoContent : HttpStatusCode.NotFound));
        }
    }
}
