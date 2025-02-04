using System.Text.Json;

namespace Beckett.Messages;

public static class MessageSerializer
{
    internal static JsonSerializerOptions Options { get; private set; } = JsonSerializerOptions.Default;

    public static void Configure(JsonSerializerOptions options)
    {
        Options = options;
    }

    public static JsonElement Serialize(Type messageType, object message)
    {
        using var document = JsonSerializer.SerializeToDocument(message, messageType, Options);

        return document.RootElement.Clone();
    }

    public static object? Deserialize(string type, JsonElement data)
    {
        var result = MessageUpcaster.Upcast(type, data);

        return !MessageTypeMap.TryGetType(result.TypeName, out var messageType)
            ? null
            : result.Data.Deserialize(messageType, Options);
    }
}
