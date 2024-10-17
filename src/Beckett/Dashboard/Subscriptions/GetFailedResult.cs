namespace Beckett.Dashboard.Subscriptions;

public record GetFailedResult(List<GetFailedResult.Failure> Failures, int TotalResults)
{
    public record Failure(long Id, string GroupName, string Name, string StreamName, long StreamPosition);
}
