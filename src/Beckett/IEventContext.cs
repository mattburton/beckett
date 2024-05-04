namespace Beckett;

public interface IEventContext
{
    Guid Id { get; }
    string StreamName { get; }
    long StreamPosition { get; }
    long GlobalPosition { get; }
    Type Type { get; }
    object Data { get; }
    IDictionary<string, object> Metadata { get; }
    DateTimeOffset Timestamp { get; }
}
