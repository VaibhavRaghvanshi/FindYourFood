using FindYourFood.Payment.Domain;
using Xunit;

namespace FindYourFood.Payment.UnitTests;

public sealed class PaymentStateTests
{
    [Fact]
    public void CancellationDuringCaptureRequiresCompensationAndClaimsHaveDurableDelay()
    {
        var now = DateTimeOffset.UtcNow;
        var intent = new PaymentIntent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, 15900, "INR", now.AddMinutes(15), now);
        var id = Guid.NewGuid();
        Assert.True(intent.Begin(id));
        Assert.False(intent.Begin(Guid.NewGuid()));
        intent.Cancel();
        intent.Complete(id, true);
        Assert.Equal(PaymentState.RefundPending, intent.State);
        intent.Refunded(id);
        Assert.Equal(PaymentState.Refunded, intent.State);
        Assert.False(intent.Begin(Guid.NewGuid()));
        var attempt = new PaymentAttempt(id, intent.Id, Guid.NewGuid(), true, now);
        Assert.True(attempt.Claim(now));
        Assert.False(attempt.Claim(now.AddSeconds(29)));
        Assert.True(attempt.Claim(now.AddSeconds(30)));
        Assert.True(attempt.Complete(true, "server-reference", now));
        Assert.False(attempt.Complete(false, "ignored", now));
        Assert.False(attempt.Claim(now.AddHours(1)));
    }
}
