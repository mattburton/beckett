namespace Beckett.Dashboard.Subscriptions;

public record GetRetriesResult(List<GetRetriesResult.Retry> Retries)
{
    public record Retry(string Application, string Name, string StreamName, long StreamPosition, Guid RetryId);
}
