using System.Text.Json;

namespace TaskHub.Infrastructure.Tests;

public class NullMessageContext(object message) : IMessageContext
{
    public string Id => null!;
    public string StreamName => null!;
    public long StreamPosition => 0;
    public long GlobalPosition => 0;
    public string Type => null!;
    public JsonDocument Data => null!;
    public JsonDocument Metadata => null!;
    public DateTimeOffset Timestamp => DateTimeOffset.MinValue;
    public Type MessageType => message.GetType();
    public object Message => message;
    public Dictionary<string, string> MessageMetadata => [];
}
