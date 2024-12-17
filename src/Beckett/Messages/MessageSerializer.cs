using System.Text.Json;

namespace Beckett.Messages;

public static class MessageSerializer
{
    internal static JsonSerializerOptions Options { get; private set; } = JsonSerializerOptions.Default;

    public static void Configure(JsonSerializerOptions options)
    {
        Options = options;
    }

    public static JsonDocument Serialize(Type messageType, object message)
    {
        return JsonSerializer.SerializeToDocument(message, messageType, Options);
    }

    public static object? Deserialize(string type, JsonDocument data)
    {
        var result = MessageUpcaster.Upcast(type, data);

        return !MessageTypeMap.TryGetType(result.TypeName, out var messageType)
            ? null
            : result.Data.Deserialize(messageType, Options);
    }
}
