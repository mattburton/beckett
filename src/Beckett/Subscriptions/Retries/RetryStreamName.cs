namespace Beckett.Subscriptions.Retries;

public static class RetryStreamName
{
    public static string For(string subscriptionName, string streamName)
    {
        return $"$subscription-retry-{subscriptionName}-{streamName}";
    }
}
