namespace Beckett.Dashboard.MessageStore;

public record GetStreamMessagesResult(List<GetStreamMessagesResult.Message> Messages, int TotalResults)
{
    public record Message(Guid Id, int StreamPosition, string Type, DateTimeOffset Timestamp);
}
