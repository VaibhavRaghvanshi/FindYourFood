using FindYourFood.Ordering.Domain;

namespace FindYourFood.Ordering.Application.Interfaces;

public interface IOrderRepository
{
    Task<Order?> FindAsync(Guid id, CancellationToken ct);
    Task<Order?> ByKeyAsync(string scope, Guid key, CancellationToken ct);
    Task<Order?> ByCartAsync(Guid cart, long revision, CancellationToken ct);
    Task<IReadOnlyList<Order>> PageAsync(IReadOnlyList<string> scopes, int page, CancellationToken ct);
    Task<CheckoutSaga?> SagaAsync(Guid order, CancellationToken ct);
    Task<IReadOnlyList<Guid>> ExpiredAsync(DateTimeOffset now, CancellationToken ct);
    void Add(Order order);
    void Add(CheckoutSaga saga);
    void Add(OrderOutboxMessage message);
}
