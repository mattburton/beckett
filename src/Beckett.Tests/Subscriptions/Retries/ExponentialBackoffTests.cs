using Beckett.Subscriptions.Retries;

namespace Beckett.Tests.Subscriptions.Retries;

public class ExponentialBackoffTests
{
    [Theory]
    [InlineData(0, 5000)]
    [InlineData(1, 10000)]
    [InlineData(2, 20000)]
    [InlineData(3, 40000)]
    [InlineData(4, 80000)]
    [InlineData(5, 160000)]
    [InlineData(6, 240000)]
    [InlineData(7, 240000)]
    [InlineData(8, 240000)]
    [InlineData(9, 240000)]
    [InlineData(10, 240000)]
    public void calculates_next_delay_with_exponential_backoff(int attempt, double backoff)
    {
        var low = DateTimeOffset.UtcNow;
        var high = low.AddMilliseconds(backoff + 1000);
        var actual = attempt.GetNextDelayWithExponentialBackoff(
            TimeSpan.FromSeconds(10),
            TimeSpan.FromMinutes(4)
        );

        Assert.InRange(actual, low, high);
    }
}
