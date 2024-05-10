namespace Beckett.Subscriptions.Retries;

public static class RetryStreamName
{
    public static string For(string subscriptionName, string streamName, long streamPosition)
    {
        return $"$retry-{subscriptionName}-{streamName}-{streamPosition}";
    }
}
