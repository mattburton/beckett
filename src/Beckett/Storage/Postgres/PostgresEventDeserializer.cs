using System.Text.Json;
using Beckett.Events;
using Beckett.Storage.Postgres.Types;

namespace Beckett.Storage.Postgres;

internal static class PostgresEventDeserializer
{
    public static object Deserialize(StreamEvent @event)
    {
        var type = EventTypeMap.GetType(@event.Type) ?? throw new Exception($"Unknown event type: {@event.Type}");

        return JsonSerializer.Deserialize(@event.Data, type) ?? throw new Exception(
            $"Unable to deserialize event data for type {@event.Type}: {@event.Data}"
        );
    }

    public static (Type Type, object Event, Dictionary<string, object> Metadata) DeserializeAll(StreamEvent @event)
    {
        var type = EventTypeMap.GetType(@event.Type) ?? throw new Exception($"Unknown event type: {@event.Type}");

        var actualEvent = JsonSerializer.Deserialize(@event.Data, type) ?? throw new Exception(
            $"Unable to deserialize event data for type {@event.Type}: {@event.Data}"
        );

        var metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(@event.Metadata) ?? throw new Exception(
            $"Unable to deserialize event metadata for type {@event.Type}: {@event.Data}"
        );

        return (type, actualEvent, metadata);
    }
}
