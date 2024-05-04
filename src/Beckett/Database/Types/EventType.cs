using Beckett.Events;
using UUIDNext;

namespace Beckett.Database.Types;

public class EventType
{
    public Guid Id { get; init; }
    public string Type { get; init; } = null!;
    public string Data { get; init; } = null!;
    public string Metadata { get; init; } = null!;

    public static string DataTypeNameFor(string schema) => $"{schema}.event[]";

    public static EventType From(object @event, Dictionary<string, object> metadata, IEventSerializer eventSerializer)
    {
        var result = eventSerializer.Serialize(@event, metadata);

        return new EventType
        {
            Id = Uuid.NewDatabaseFriendly(UUIDNext.Database.PostgreSql),
            Type = result.TypeName,
            Data = result.Data,
            Metadata = result.Metadata
        };
    }
}
