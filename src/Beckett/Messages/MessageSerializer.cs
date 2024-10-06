using System.Text.Json;

namespace Beckett.Messages;

public static class MessageSerializer
{
    private static JsonSerializerOptions _options = JsonSerializerOptions.Default;

    public static void Configure(JsonSerializerOptions options)
    {
        _options = options;
    }

    public static JsonDocument Serialize(object message, Type? messageType = null)
    {
        if (message is Message genericMessage)
        {
            return genericMessage.Data;
        }

        return JsonSerializer.SerializeToDocument(message, messageType ?? message.GetType(), _options);
    }

    public static object? Deserialize(string type, JsonDocument data)
    {
        return !MessageTypeMap.TryGetType(type, out var messageType) ? null : data.Deserialize(messageType!, _options);
    }
}
