namespace FindYourFood.Cart.Domain;

public sealed class ShoppingCart(Guid id, Guid branchId, Guid? customerId, string? guestKeyHash, DateTimeOffset expiresAt)
{
    public Guid Id { get; private set; } = id;
    public Guid BranchId { get; private set; } = branchId;
    public Guid? CustomerId { get; private set; } = customerId;
    public string? GuestKeyHash { get; private set; } = guestKeyHash;
    public DateTimeOffset ExpiresAt { get; private set; } = expiresAt;
    public long Revision { get; private set; } = 1;
    public string SelectionsJson { get; private set; } = "[]";

    public void Replace(string selections)
    {
        SelectionsJson = selections;
        Revision++;
    }
}
