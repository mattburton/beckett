using Beckett.Database.Models;

namespace Beckett.Database;

public interface IPostgresMessageDeserializer
{
    object Deserialize(PostgresMessage message);

    (Type Type, object Message, Dictionary<string, object> Metadata) DeserializeAll(PostgresMessage message);

    (object Message, Dictionary<string, object> Metadata) DeserializeAll(PostgresRecurringMessage message);

    (object Message, Dictionary<string, object> Metadata) DeserializeAll(PostgresScheduledMessage message);
}
