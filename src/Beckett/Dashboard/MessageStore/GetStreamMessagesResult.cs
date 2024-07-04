namespace Beckett.Dashboard.MessageStore;

public record GetStreamMessagesResult(List<GetStreamMessagesResult.Message> Messages)
{
    public record Message(Guid Id, int StreamPosition, string Type, DateTimeOffset Timestamp);
}
