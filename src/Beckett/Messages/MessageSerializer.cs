using System.Text.Json;

namespace Beckett.Messages;

public static class MessageSerializer
{
    private static JsonSerializerOptions _options = JsonSerializerOptions.Default;

    public static void Configure(JsonSerializerOptions options)
    {
        _options = options;
    }

    public static JsonDocument Serialize(Type messageType, object message)
    {
        return JsonSerializer.SerializeToDocument(message, messageType, _options);
    }

    public static object? Deserialize(string type, JsonDocument data)
    {
        return !MessageTypeMap.TryGetType(type, out var messageType)
            ? null
            : MessageTransformer.Transform(type, data).Deserialize(messageType!, _options);
    }
}
