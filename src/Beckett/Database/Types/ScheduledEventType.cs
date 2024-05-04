using Beckett.Events;
using UUIDNext;

namespace Beckett.Database.Types;

public class ScheduledEventType
{
    public Guid Id { get; init; }
    public string Type { get; init; } = null!;
    public string Data { get; init; } = null!;
    public string Metadata { get; init; } = null!;
    public DateTimeOffset DeliverAt { get; init; } = DateTimeOffset.UtcNow;

    public static string DataTypeNameFor(string schema) => $"{schema}.scheduled_event[]";

    public static ScheduledEventType From(
        object @event,
        Dictionary<string, object> metadata,
        DateTimeOffset deliverAt,
        IEventSerializer eventSerializer
    )
    {
        var result = eventSerializer.Serialize(@event, metadata);

        return new ScheduledEventType
        {
            Id = Uuid.NewDatabaseFriendly(UUIDNext.Database.PostgreSql),
            Type = result.TypeName,
            Data = result.Data,
            Metadata = result.Metadata,
            DeliverAt = deliverAt
        };
    }
}
