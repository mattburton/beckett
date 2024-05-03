using Beckett.Events;
using UUIDNext;

namespace Beckett.Storage.Postgres.Types;

public class NewEvent
{
    public Guid Id { get; init; }
    public string Type { get; init; } = null!;
    public string Data { get; init; } = null!;
    public string Metadata { get; init; } = null!;

    public static string DataTypeNameFor(string schema) => $"{schema}.new_event[]";

    public static NewEvent From(object @event, Dictionary<string, object> metadata, IEventSerializer eventSerializer)
    {
        var result = eventSerializer.Serialize(@event, metadata);

        return new NewEvent
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            Type = result.TypeName,
            Data = result.Data,
            Metadata = result.Metadata
        };
    }
}
