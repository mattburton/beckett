using System.Text.Json;

namespace Beckett.Messages;

public class MessageSerializer(IMessageTypeMap messageTypeMap) : IMessageSerializer
{
    public (Type Type, string TypeName, string Data, string Metadata) Serialize(
        object message,
        Dictionary<string, object> metadata
    )
    {
        var type = message.GetType();
        var typeName = messageTypeMap.GetName(type);
        var dataJson = JsonSerializer.Serialize(message);
        var metadataJson = JsonSerializer.Serialize(metadata);

        return (type, typeName, dataJson, metadataJson);
    }
}
