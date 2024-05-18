namespace Beckett;

public interface IMessageContext : IMessageStore, IMessageScheduler
{
    Guid Id { get; }
    string StreamName { get; }
    long StreamPosition { get; }
    long GlobalPosition { get; }
    Type Type { get; }
    object Message { get; }
    IDictionary<string, object> Metadata { get; }
    DateTimeOffset Timestamp { get; }
    IServiceProvider Services { get; }
}
