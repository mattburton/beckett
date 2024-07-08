namespace Beckett.Subscriptions.Retries;

public static class ExponentialBackoff
{
    private static readonly Random Random = new();

    public static DateTimeOffset GetNextDelayWithExponentialBackoff(this int attempt, TimeSpan delay, TimeSpan maxDelay)
    {
        var jitter = Random.Next(1000);

        var result = Math.Min(delay.TotalMilliseconds * Math.Pow(2, attempt) / 2, maxDelay.TotalMilliseconds) + jitter;

        return DateTimeOffset.UtcNow.Add(TimeSpan.FromMilliseconds(result));
        ;
    }
}
