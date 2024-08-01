namespace Beckett.Subscriptions.Retries;

public static class RetryStreamName
{
    public static string For(long checkpointId) => $"$retry-{checkpointId}";
}
