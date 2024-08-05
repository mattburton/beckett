using Beckett.Database.Models;

namespace Beckett.MessageStorage.Postgres;

public interface IPostgresMessageDeserializer
{
    MessageResult Deserialize(PostgresMessage message);

    (object Message, Dictionary<string, object> Metadata) Deserialize(PostgresRecurringMessage message);

    (object Message, Dictionary<string, object> Metadata) Deserialize(PostgresScheduledMessage message);
}
