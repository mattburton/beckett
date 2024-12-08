using System.Text.Json;
using System.Text.Json.Serialization;

namespace Beckett.Messages;

public static class MessageSerializer
{
    internal static JsonSerializerOptions Options { get; private set; } = BuildJsonSerializerOptions();

    public static void Configure(JsonSerializerOptions options)
    {
        options.TypeInfoResolverChain.Add(BeckettJsonSerializerContext.Default);

        Options = options;
    }

    public static JsonDocument Serialize(Type messageType, object message)
    {
        return JsonSerializer.SerializeToDocument(message, messageType, Options);
    }

    public static object? Deserialize(string type, JsonDocument data)
    {
        return !MessageTypeMap.TryGetType(type, out var messageType)
            ? null
            : MessageTransformer.Transform(type, data).Deserialize(messageType!, Options);
    }

    private static JsonSerializerOptions BuildJsonSerializerOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerOptions.Default);

        options.TypeInfoResolverChain.Add(BeckettJsonSerializerContext.Default);

        return options;
    }
}

[JsonSerializable(typeof(Dictionary<string, string>))]
public partial class BeckettJsonSerializerContext : JsonSerializerContext;
