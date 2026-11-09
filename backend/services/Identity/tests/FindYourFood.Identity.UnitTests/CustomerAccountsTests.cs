using FindYourFood.Identity.Application;
using FindYourFood.Identity.Domain;
using Xunit;

namespace FindYourFood.Identity.UnitTests;

public sealed class CustomerAccountsTests
{
    [Theory]
    [InlineData(null, "a sufficiently long password")]
    [InlineData("customer @example.test", "a sufficiently long password")]
    [InlineData("bad-email", "a sufficiently long password")]
    [InlineData("customer@example.test", "short")]
    [InlineData("customer@example.test", null)]
    public async Task Invalid_registration_never_calls_the_store(string? email, string? password)
    {
        var store = new TestStore();
        var result = await new CustomerAccounts(store).RegisterAsync(email, password);
        Assert.Equal(RegistrationResult.InvalidInput, result);
        Assert.Null(store.Email);
    }

    [Fact]
    public async Task Registration_trims_email_and_preserves_password()
    {
        var store = new TestStore();
        var result = await new CustomerAccounts(store)
            .RegisterAsync("  Customer@example.test  ", "  a long passphrase  ");
        Assert.Equal(RegistrationResult.Created, result);
        Assert.Equal("Customer@example.test", store.Email?.Value);
        Assert.Equal("  a long passphrase  ", store.Password);
    }

    [Fact]
    public async Task Duplicate_registration_is_reported()
    {
        var store = new TestStore { Created = false };
        var result = await new CustomerAccounts(store)
            .RegisterAsync("customer@example.test", "a valid passphrase");
        Assert.Equal(RegistrationResult.AlreadyRegistered, result);
    }

    [Fact]
    public async Task Oversized_sign_in_password_never_calls_the_store()
    {
        var result = await new CustomerAccounts(new TestStore())
            .AuthenticateAsync("customer@example.test", new string('x', 129));
        Assert.Null(result);
    }

    private sealed class TestStore : ICustomerAccountStore
    {
        public bool Created { get; init; } = true;
        public CustomerEmail? Email { get; private set; }
        public string? Password { get; private set; }

        public Task<bool> CreateAsync(CustomerEmail email, string password)
        {
            Email = email;
            Password = password;
            return Task.FromResult(Created);
        }

        public Task<CustomerAccount?> AuthenticateAsync(CustomerEmail email, string password) =>
            throw new NotSupportedException();

        public Task<CustomerAccount?> FindAsync(Guid id) => throw new NotSupportedException();
    }
}
