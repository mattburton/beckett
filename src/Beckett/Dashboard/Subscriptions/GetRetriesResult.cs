namespace Beckett.Dashboard.Subscriptions;

public record GetRetriesResult(List<GetRetriesResult.Retry> Retries)
{
    public record Retry(
        long Id,
        string GroupName,
        string Name,
        string StreamName,
        long StreamPosition
    );
}
