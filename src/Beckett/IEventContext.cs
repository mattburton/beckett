namespace Beckett;

public interface IEventContext
{
    Guid Id { get; init; }
    string StreamName { get; init; }
    long StreamPosition { get; init; }
    long GlobalPosition { get; init; }
    Type Type { get; init; }
    object Data { get; init; }
    IDictionary<string, object> Metadata { get; init; }
    DateTimeOffset Timestamp { get; init; }
}
