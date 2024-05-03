namespace Beckett.Events.Scheduling;

public interface IScheduledEventContext
{
    string StreamName { get; }
    object Data { get; }
    Dictionary<string, object> Metadata { get; }
}
