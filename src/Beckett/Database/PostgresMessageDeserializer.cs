using System.Text.Json;
using Beckett.Database.Models;
using Beckett.Messages;

namespace Beckett.Database;

public class PostgresMessageDeserializer(IMessageTypeMap messageTypeMap) : IPostgresMessageDeserializer
{
    public object Deserialize(PostgresMessage message)
    {
        var type = messageTypeMap.GetType(message.Type) ??
                   throw new Exception($"Unknown message type: {message.Type}");

        return JsonSerializer.Deserialize(message.Data, type) ?? throw new Exception(
            $"Unable to deserialize message data for type {message.Type}: {message.Data}"
        );
    }

    public (Type Type, object Message, Dictionary<string, object> Metadata) DeserializeAll(PostgresMessage message)
    {
        var type = messageTypeMap.GetType(message.Type) ??
                   throw new Exception($"Unknown message type: {message.Type}");

        var deserializedMessage = JsonSerializer.Deserialize(message.Data, type) ?? throw new Exception(
            $"Unable to deserialize message data for type {message.Type}: {message.Data}"
        );

        var metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(message.Metadata) ?? throw new Exception(
            $"Unable to deserialize message metadata for type {message.Type}: {message.Data}"
        );

        return (type, deserializedMessage, metadata);
    }

    public (object Message, Dictionary<string, object> Metadata) DeserializeAll(PostgresScheduledMessage message)
    {
        var type = messageTypeMap.GetType(message.Type) ??
                   throw new Exception($"Unknown scheduled message type: {message.Type}");

        var deserializedMessage = JsonSerializer.Deserialize(message.Data, type) ?? throw new Exception(
            $"Unable to deserialize scheduled message data for type {message.Type}: {message.Data}"
        );

        var metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(message.Metadata) ?? throw new Exception(
            $"Unable to deserialize scheduled message metadata for type {message.Type}: {message.Data}"
        );

        return (deserializedMessage, metadata);
    }
}
