using Beckett.Messages;
using UUIDNext;

namespace Beckett.Database.Types;

public class MessageType
{
    public Guid Id { get; init; }
    public string Type { get; init; } = null!;
    public string Data { get; init; } = null!;
    public string Metadata { get; init; } = null!;

    public static string DataTypeNameFor(string schema) => $"{schema}.message[]";

    public static MessageType From(
        object message,
        Dictionary<string, object> metadata,
        IMessageSerializer messageSerializer
    )
    {
        var result = messageSerializer.Serialize(message, metadata);

        return new MessageType
        {
            Id = Uuid.NewDatabaseFriendly(UUIDNext.Database.PostgreSql),
            Type = result.TypeName,
            Data = result.Data,
            Metadata = result.Metadata
        };
    }
}