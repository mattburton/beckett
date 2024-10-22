namespace Beckett.Dashboard.MessageStore;

public record GetCorrelatedMessagesResult(List<GetCorrelatedMessagesResult.Message> Messages, int TotalResults)
{
    public record Message(Guid Id, string StreamName, int StreamPosition, string Type, DateTimeOffset Timestamp);
}
