namespace Beckett.Messages.Scheduling;

public interface IScheduledMessageContext
{
    string Topic { get; }
    string StreamId { get; }
    object Message { get; }
    Dictionary<string, object> Metadata { get; }
}
