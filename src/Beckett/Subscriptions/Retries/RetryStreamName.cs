namespace Beckett.Subscriptions.Retries;

public static class RetryStreamName
{
    public static string For(Guid id) => $"$retry-{id}";
}
