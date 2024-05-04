using Beckett.Events;
using UUIDNext;

namespace Beckett.Storage.Postgres.Types;

public class NewScheduledEvent
{
    public Guid Id { get; init; }
    public string Type { get; init; } = null!;
    public string Data { get; init; } = null!;
    public string Metadata { get; init; } = null!;
    public DateTimeOffset DeliverAt { get; init; } = DateTimeOffset.UtcNow;

    public static string DataTypeNameFor(string schema) => $"{schema}.new_scheduled_event[]";

    public static NewScheduledEvent From(
        object @event,
        Dictionary<string, object> metadata,
        DateTimeOffset deliverAt,
        EventSerializer eventSerializer
    )
    {
        var result = eventSerializer.Serialize(@event, metadata);

        return new NewScheduledEvent
        {
            Id = Uuid.NewDatabaseFriendly(Database.PostgreSql),
            Type = result.TypeName,
            Data = result.Data,
            Metadata = result.Metadata,
            DeliverAt = deliverAt
        };
    }
}
