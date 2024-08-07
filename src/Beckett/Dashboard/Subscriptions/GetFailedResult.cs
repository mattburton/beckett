namespace Beckett.Dashboard.Subscriptions;

public record GetFailedResult(List<GetFailedResult.Failure> Failures)
{
    public record Failure(string GroupName, string Name, string StreamName, long StreamPosition, Guid RetryId);
}
