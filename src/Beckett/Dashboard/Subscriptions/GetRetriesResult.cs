namespace Beckett.Dashboard.Subscriptions;

public record GetRetriesResult(List<GetRetriesResult.Retry> Retries, int TotalResults)
{
    public record Retry(
        long Id,
        string GroupName,
        string Name,
        string StreamName,
        long StreamPosition
    );
}
