namespace Beckett.Dashboard.Subscriptions;

public record GetRetriesResult(List<GetRetriesResult.Retry> Retries)
{
    public record Retry(
        string GroupName,
        string Name,
        string StreamName,
        long StreamPosition,
        Guid RetryId,
        int Attempts,
        DateTimeOffset? RetryAt
    )
    {
        public string RetryIn => RetryAt == null ? "" : RetryAt.Value.ToRetryTimeDisplay();
    }
}
