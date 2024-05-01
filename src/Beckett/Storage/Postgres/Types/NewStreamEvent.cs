using Beckett.Events;
using UUIDNext;

namespace Beckett.Storage.Postgres.Types;

public class NewStreamEvent
{
    public Guid Id { get; init; }
    public string Type { get; init; } = null!;
    public string Data { get; init; } = null!;
    public string Metadata { get; init; } = null!;
    public DateTimeOffset? DeliverAt { get; init; }

    public static string DataTypeNameFor(string schema) => $"{schema}.new_stream_event[]";

    public static NewStreamEvent From(object @event, Dictionary<string, object> metadata, DateTimeOffset? deliverAt)
    {
        var result = EventSerializer.Serialize(@event, metadata);

        return new NewStreamEvent
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            Type = result.TypeName,
            Data = result.Data,
            Metadata = result.Metadata,
            DeliverAt = deliverAt
        };
    }
}
