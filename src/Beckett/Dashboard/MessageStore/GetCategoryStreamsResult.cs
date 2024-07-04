namespace Beckett.Dashboard.MessageStore;

public record GetCategoryStreamsResult(List<GetCategoryStreamsResult.Stream> Streams)
{
    public record Stream(string StreamName, DateTimeOffset LastUpdated);
}
