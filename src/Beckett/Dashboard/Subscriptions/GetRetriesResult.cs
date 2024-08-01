namespace Beckett.Dashboard.Subscriptions;

public record GetRetriesResult(List<GetRetriesResult.Retry> Retries)
{
    public record Retry(long CheckpointId, string GroupName, string Name, string StreamName, long StreamPosition);
}
