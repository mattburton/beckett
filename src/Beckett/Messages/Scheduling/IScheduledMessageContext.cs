namespace Beckett.Messages.Scheduling;

public interface IScheduledMessageContext
{
    string StreamName { get; }
    object Message { get; }
    Dictionary<string, object> Metadata { get; }
}
