using Beckett.Database.Models;
using Beckett.Messages.Storage;

namespace Beckett.Database;

public interface IPostgresMessageDeserializer
{
    MessageResult Deserialize(PostgresMessage message);

    (object Message, Dictionary<string, object> Metadata) Deserialize(PostgresRecurringMessage message);

    (object Message, Dictionary<string, object> Metadata) Deserialize(PostgresScheduledMessage message);
}
