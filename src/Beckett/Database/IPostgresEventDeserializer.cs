using Beckett.Database.Models;

namespace Beckett.Database;

public interface IPostgresEventDeserializer
{
    object Deserialize(PostgresEvent @event);

    (Type Type, object Event, Dictionary<string, object> Metadata) DeserializeAll(PostgresEvent @event);

    (object Event, Dictionary<string, object> Metadata) DeserializeAll(PostgresScheduledEvent @event);
}
