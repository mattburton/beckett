namespace Beckett.Events;

public readonly record struct EventContext(
    Guid Id,
    string StreamName,
    long StreamPosition,
    long GlobalPosition,
    Type Type,
    object Data,
    IDictionary<string, object> Metadata,
    DateTimeOffset Timestamp
) : IEventContext;
