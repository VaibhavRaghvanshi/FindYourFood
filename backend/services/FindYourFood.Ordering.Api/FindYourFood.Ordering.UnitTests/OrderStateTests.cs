using FindYourFood.Ordering.Domain;
using Xunit;

namespace FindYourFood.Ordering.UnitTests;

public sealed class OrderStateTests
{
    [Fact]
    public void EnforcesThePaidPreparationAndDeliverySequence()
    {
        var now = DateTimeOffset.UtcNow;
        var order = new Order(Guid.NewGuid(), Guid.NewGuid(), 1, Guid.NewGuid(), "test", null, null, "hash", "{}", now);
        Assert.False(order.MoveTo(OrderStage.Delivered, now));
        foreach (var stage in new[] { OrderStage.Placed, OrderStage.Accepted, OrderStage.Preparing,
            OrderStage.ReadyForPickup, OrderStage.OutForDelivery, OrderStage.Delivered })
        {
            Assert.True(order.MoveTo(stage, now));
        }
        Assert.False(order.MoveTo(OrderStage.Cancelled, now));
        var saga = new CheckoutSaga(order.Id, now.AddMinutes(15));
        saga.Cancel();
        var payment = Guid.NewGuid();
        Assert.True(saga.ConfirmPayment(payment));
        Assert.Equal(CheckoutState.Compensating, saga.State);
        Assert.False(saga.ConfirmPayment(payment));
    }
}
