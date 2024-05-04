using System.Text.Json;

namespace Beckett.Events;

public class EventSerializer(EventTypeMap eventTypeMap)
{
    public (Type Type, string TypeName, string Data, string Metadata) Serialize(
        object @event,
        Dictionary<string, object> metadata
    )
    {
        var type = @event.GetType();
        var typeName = eventTypeMap.GetName(type);
        var dataJson = JsonSerializer.Serialize(@event);
        var metadataJson = JsonSerializer.Serialize(metadata);

        return (type, typeName, dataJson, metadataJson);
    }
}
