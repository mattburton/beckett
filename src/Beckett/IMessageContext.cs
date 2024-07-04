namespace Beckett;

public interface IMessageContext
{
    string Id { get; }
    string StreamName { get; }
    long StreamPosition { get; }
    long GlobalPosition { get; }
    Type Type { get; }
    object Message { get; }
    IDictionary<string, object> Metadata { get; }
    DateTimeOffset Timestamp { get; }
    IServiceProvider Services { get; }
}
