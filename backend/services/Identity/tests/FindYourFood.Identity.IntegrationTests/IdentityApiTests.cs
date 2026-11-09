using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace FindYourFood.Identity.IntegrationTests;

public sealed class IdentityApiTests(IdentitySqlFixture fixture) : IClassFixture<IdentitySqlFixture>
{
    private const string Root = "/api/v1/identity";
    private const string Password = "a strong test passphrase";

    [Fact]
    public async Task Migrated_database_is_ready_and_openapi_exposes_identity_routes()
    {
        using var client = fixture.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/ready")).StatusCode);
        var document = await client.GetFromJsonAsync<JsonElement>("/openapi/v1.json");
        Assert.True(document.GetProperty("paths").TryGetProperty(Root + "/login", out _));
    }

    [Fact]
    public async Task Registered_customer_can_sign_in_and_read_own_profile()
    {
        using var client = fixture.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(Root + "/me")).StatusCode);
        var email = $"customer-{Guid.NewGuid():N}@example.test";
        Assert.Equal(HttpStatusCode.Created, (await Post(client, "register", email, Password)).StatusCode);
        using var login = await Post(client, "login", email.ToUpperInvariant(), Password);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.True(login.Headers.CacheControl?.NoStore);
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var profile = await client.GetFromJsonAsync<JsonElement>(Root + "/me");
        Assert.Equal(email, profile.GetProperty("email").GetString());
        Assert.False(profile.GetProperty("emailVerified").GetBoolean());

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token![..^10] + "Invalid999");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(Root + "/me")).StatusCode);
    }

    [Fact]
    public async Task Concurrent_duplicate_registration_creates_only_one_account()
    {
        using var client = fixture.CreateClient();
        var email = $"duplicate-{Guid.NewGuid():N}@example.test";
        var responses = await Task.WhenAll(Post(client, "register", email, Password),
            Post(client, "register", email.ToUpperInvariant(), Password));
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Created);
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Invalid_registration_and_account_lockout_are_enforced()
    {
        using var client = fixture.CreateClient();
        var email = $"lockout-{Guid.NewGuid():N}@example.test";
        Assert.Equal(HttpStatusCode.BadRequest, (await Post(client, "register", email, "short")).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await Post(client, "register", email, Password)).StatusCode);
        for (var attempt = 0; attempt < 5; attempt++)
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await Post(client, "login", email, "wrong password")).StatusCode);
        }

        Assert.Equal(HttpStatusCode.Unauthorized, (await Post(client, "login", email, Password)).StatusCode);
    }

    private static Task<HttpResponseMessage> Post(HttpClient client, string action, string email, string password) =>
        client.PostAsJsonAsync($"{Root}/{action}", new { email, password });
}
