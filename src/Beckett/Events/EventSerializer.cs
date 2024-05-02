using System.Text.Json;

namespace Beckett.Events;

internal static class EventSerializer
{
    public static (Type Type, string TypeName, string Data, string Metadata) Serialize(
        object @event,
        Dictionary<string, object> metadata
    )
    {
        var type = @event.GetType();
        var typeName = EventTypeMap.GetName(type);
        var dataJson = JsonSerializer.Serialize(@event);
        var metadataJson = JsonSerializer.Serialize(metadata);

        return (type, typeName, dataJson, metadataJson);
    }
}
