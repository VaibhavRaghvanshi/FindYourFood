using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using FindYourFood.Customer.Application.DTOs;
using FindYourFood.Customer.Domain;
using FindYourFood.Customer.Infrastructure.Data;
using FindYourFood.Testing;
using Xunit;

namespace FindYourFood.Customer.IntegrationTests;

public sealed class CustomerFixture() : SqlServiceFixture<CustomerHost, CustomerDbContext>("Customer", options => new(options));

public sealed class CustomerApiTests(CustomerFixture fixture) : IClassFixture<CustomerFixture>
{
    [Fact]
    public async Task SettingsArePrivateAndStaleWritesCannotOverwriteThem()
    {
        const string route = "/api/v1/customer/me";
        using var anonymous = fixture.Client();
        using var staff = fixture.Client(Guid.NewGuid(), new Claim("account_type", "staff"));
        using var customer = fixture.Client(Guid.NewGuid());
        using var other = fixture.Client(Guid.NewGuid());

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(route)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await staff.GetAsync(route)).StatusCode);
        var initial = (await customer.GetFromJsonAsync<CustomerSnapshot>(route))!;
        var address = new DeliveryAddress(Guid.NewGuid(), "Home", "Synthetic street", "Pune", "411001", null, null);
        var updated = initial with
        {
            Settings = ProfileSettings.Empty with
            {
                DisplayName = "Sample Customer",
                Addresses = [address],
                PrimaryAddress = address.Id,
                DietaryPreferences = ["Vegetarian"],
                FavoriteBranches = [Guid.NewGuid()]
            }
        };
        var writes = await Task.WhenAll(customer.PutAsJsonAsync(route, updated), customer.PutAsJsonAsync(route, updated));
        Assert.Single(writes, response => response.StatusCode == HttpStatusCode.OK);
        Assert.Single(writes, response => response.StatusCode == HttpStatusCode.Conflict);
        var saved = (await customer.GetFromJsonAsync<CustomerSnapshot>(route))!;
        Assert.Equal(1, saved.Revision);
        Assert.Equal(address, Assert.Single(saved.Settings.Addresses));
        Assert.Empty((await other.GetFromJsonAsync<CustomerSnapshot>(route))!.Settings.Addresses);
    }
}
