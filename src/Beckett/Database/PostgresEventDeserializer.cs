using System.Text.Json;
using Beckett.Database.Models;
using Beckett.Events;

namespace Beckett.Database;

public class PostgresEventDeserializer(IEventTypeMap eventTypeMap) : IPostgresEventDeserializer
{
    public object Deserialize(PostgresEvent @event)
    {
        var type = eventTypeMap.GetType(@event.Type) ??
                   throw new Exception($"Unknown event type: {@event.Type}");

        return JsonSerializer.Deserialize(@event.Data, type) ?? throw new Exception(
            $"Unable to deserialize event data for type {@event.Type}: {@event.Data}"
        );
    }

    public (Type Type, object Event, Dictionary<string, object> Metadata) DeserializeAll(PostgresEvent @event)
    {
        var type = eventTypeMap.GetType(@event.Type) ??
                   throw new Exception($"Unknown event type: {@event.Type}");

        var actualEvent = JsonSerializer.Deserialize(@event.Data, type) ?? throw new Exception(
            $"Unable to deserialize event data for type {@event.Type}: {@event.Data}"
        );

        var metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(@event.Metadata) ?? throw new Exception(
            $"Unable to deserialize event metadata for type {@event.Type}: {@event.Data}"
        );

        return (type, actualEvent, metadata);
    }

    public (object Event, Dictionary<string, object> Metadata) DeserializeAll(PostgresScheduledEvent @event)
    {
        var type = eventTypeMap.GetType(@event.Type) ??
                   throw new Exception($"Unknown scheduled event type: {@event.Type}");

        var actualEvent = JsonSerializer.Deserialize(@event.Data, type) ?? throw new Exception(
            $"Unable to deserialize scheduled event data for type {@event.Type}: {@event.Data}"
        );

        var metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(@event.Metadata) ?? throw new Exception(
            $"Unable to deserialize scheduled event metadata for type {@event.Type}: {@event.Data}"
        );

        return (actualEvent, metadata);
    }
}
