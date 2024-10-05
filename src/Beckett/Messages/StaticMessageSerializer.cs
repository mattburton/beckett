using System.Text.Json;

namespace Beckett.Messages;

public static class StaticMessageSerializer
{
    public static JsonDocument Serialize(object message)
    {
        if (message is JsonDocument document)
        {
            return document;
        }

        return JsonSerializer.SerializeToDocument(message, message.GetType());
    }

    public static object? Deserialize(string type, JsonDocument data)
    {
        return !MessageTypeMap.TryGetType(type, out var messageType) ? null : data.Deserialize(messageType!);
    }
}
