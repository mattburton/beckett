namespace Beckett.Dashboard.Subscriptions;

public record GetFailedResult(List<GetFailedResult.Failure> Failures)
{
    public record Failure(long CheckpointId, string GroupName, string Name, string StreamName, long StreamPosition);
}
