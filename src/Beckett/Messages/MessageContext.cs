namespace Beckett.Messages;

public readonly record struct MessageContext(
    string Id,
    string StreamName,
    long StreamPosition,
    long GlobalPosition,
    Type Type,
    object Message,
    IDictionary<string, object> Metadata,
    DateTimeOffset Timestamp,
    IServiceProvider Services
) : IMessageContext;
