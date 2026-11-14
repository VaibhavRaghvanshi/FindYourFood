using FindYourFood.Ordering.Application.Interfaces;
using FindYourFood.Ordering.Domain;
using FindYourFood.Ordering.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FindYourFood.Ordering.Infrastructure.Repositories;

internal sealed class OrderRepository(OrderingDbContext database) : IOrderRepository
{
    public Task<Order?> FindAsync(Guid id, CancellationToken ct) =>
        database.Orders.SingleOrDefaultAsync(order => order.Id == id, ct);

    public Task<Order?> ByKeyAsync(string scope, Guid key, CancellationToken ct) =>
        database.Orders.AsNoTracking().SingleOrDefaultAsync(order => order.OwnerScope == scope && order.IdempotencyKey == key, ct);

    public Task<Order?> ByCartAsync(Guid cart, long revision, CancellationToken ct) =>
        database.Orders.AsNoTracking().SingleOrDefaultAsync(order => order.CartId == cart && order.CartRevision == revision, ct);

    public async Task<IReadOnlyList<Order>> PageAsync(IReadOnlyList<string> scopes, int page, CancellationToken ct) =>
        await database.Orders.AsNoTracking().Where(order => scopes.Contains(order.OwnerScope))
            .OrderByDescending(order => order.CreatedAt).ThenBy(order => order.Id)
            .Skip((page - 1) * 20).Take(20).ToListAsync(ct);

    public Task<CheckoutSaga?> SagaAsync(Guid order, CancellationToken ct) =>
        database.Sagas.SingleOrDefaultAsync(saga => saga.OrderId == order, ct);

    public async Task<IReadOnlyList<Guid>> ExpiredAsync(DateTimeOffset now, CancellationToken ct) =>
        await (from order in database.Orders
               join saga in database.Sagas on order.Id equals saga.OrderId
               where order.Stage == OrderStage.AwaitingPayment && saga.PaymentDeadline <= now
               orderby saga.PaymentDeadline, order.Id
               select order.Id).Take(20).ToListAsync(ct);

    public void Add(Order order) => database.Orders.Add(order);
    public void Add(CheckoutSaga saga) => database.Sagas.Add(saga);
    public void Add(OrderOutboxMessage message) => database.Outbox.Add(message);
}
