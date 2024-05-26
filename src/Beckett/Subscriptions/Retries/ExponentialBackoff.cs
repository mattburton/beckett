namespace Beckett.Subscriptions.Retries;

public static class ExponentialBackoff
{
    private static readonly Random Random = new();

    public static DateTimeOffset GetNextDelayWithExponentialBackoff(this int attempt)
    {
        var jitter = Random.Next(20);

        var delay = TimeSpan.FromSeconds(
            Math.Round(Math.Pow(attempt - 1, 4) + 10 + jitter * attempt)
        );

        return DateTimeOffset.UtcNow.Add(delay);
    }
}
