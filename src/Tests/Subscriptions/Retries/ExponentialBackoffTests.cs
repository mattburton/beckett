using Beckett.Subscriptions.Retries;

namespace Tests.Subscriptions.Retries;

public class ExponentialBackoffTests
{
    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 1)]
    [InlineData(3, 2)]
    [InlineData(4, 4)]
    [InlineData(5, 10)]
    [InlineData(6, 20)]
    [InlineData(7, 30)]
    [InlineData(8, 60)]
    [InlineData(9, 90)]
    [InlineData(10, 120)]
    public void CalculatesNextDelayWithExponentialBackoff(int attempt, int minutes)
    {
        var low = DateTimeOffset.UtcNow;
        var high = low.AddMinutes(minutes);
        var actual = attempt.GetNextDelayWithExponentialBackoff();

        Assert.InRange(actual, low, high);
    }
}
