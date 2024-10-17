namespace Beckett.Dashboard.MessageStore;

public record GetCategoryStreamsResult(List<GetCategoryStreamsResult.Stream> Streams, int TotalResults)
{
    public record Stream(string StreamName, DateTimeOffset LastUpdated);
}
