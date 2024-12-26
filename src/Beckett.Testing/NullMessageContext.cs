using System.Text.Json;

namespace Beckett.Testing;

public class NullMessageContext : IMessageContext
{
    public string Id { get; } = null!;
    public string StreamName { get; } = null!;
    public long StreamPosition { get; } = 0;
    public long GlobalPosition { get; } = 0;
    public string Type { get; } = null!;
    public JsonDocument Data { get; } = null!;
    public JsonDocument Metadata { get; } = null!;
    public DateTimeOffset Timestamp { get; } = DateTimeOffset.UtcNow;
    public Type? MessageType { get; } = null;
    public object? Message { get; } = null;
    public Dictionary<string, string> MessageMetadata { get; } = null!;

    public static IMessageContext Instance { get; } = new NullMessageContext();
}
