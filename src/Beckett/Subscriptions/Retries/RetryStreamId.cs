namespace Beckett.Subscriptions.Retries;

public static class RetryStreamId
{
    public static string For(string subscriptionName, string topic, string streamId, long streamPosition)
    {
        return $"{subscriptionName}-{topic}-{streamId}-{streamPosition}";
    }
}
