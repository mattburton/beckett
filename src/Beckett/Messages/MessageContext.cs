using System.Text.Json;

namespace Beckett.Messages;

public readonly record struct MessageContext(
    string Id,
    string StreamName,
    long StreamPosition,
    long GlobalPosition,
    string Type,
    JsonDocument Data,
    JsonDocument Metadata,
    DateTimeOffset Timestamp,
    IServiceProvider Services
) : IMessageContext
{
    public Lazy<Type?> ResolvedType { get; } = new(MessageTypeMap.TryGetType(Type, out var type) ? type : null);

    public Lazy<object?> ResolvedMessage { get; } = new(MessageSerializer.Deserialize(Type, Data));

    public Lazy<Dictionary<string, object>?> ResolvedMetadata { get; } = new(Metadata.ToMetadataDictionary());
}
