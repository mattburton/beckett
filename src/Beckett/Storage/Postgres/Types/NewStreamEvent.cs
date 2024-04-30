using Beckett.Events;
using UUIDNext;

namespace Beckett.Storage.Postgres.Types;

public class NewStreamEvent
{
    public const string DataTypeName = "new_stream_event[]";

    public Guid Id { get; init; }
    public string Type { get; init; } = null!;
    public string Data { get; init; } = null!;
    public string Metadata { get; init; } = null!;

    public static NewStreamEvent From(object @event, Dictionary<string, object> metadata)
    {
        var result = EventSerializer.Serialize(@event, metadata);

        return new NewStreamEvent
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            Type = result.TypeName,
            Data = result.Data,
            Metadata = result.Metadata
        };
    }
}
