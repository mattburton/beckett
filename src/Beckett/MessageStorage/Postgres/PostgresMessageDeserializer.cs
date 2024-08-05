using System.Text.Json;
using Beckett.Database.Models;
using Beckett.Messages;

namespace Beckett.MessageStorage.Postgres;

public class PostgresMessageDeserializer(IMessageTypeMap messageTypeMap) : IPostgresMessageDeserializer
{
    public MessageResult Deserialize(PostgresMessage message)
    {
        var type = messageTypeMap.GetType(message.Type) ??
                   throw new Exception($"Unknown message type: {message.Type}");

        var data = JsonSerializer.Deserialize(message.Data, type) ?? throw new Exception(
            $"Unable to deserialize message data for type {message.Type}: {message.Data}"
        );

        var metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(message.Metadata) ?? throw new Exception(
            $"Unable to deserialize message metadata for type {message.Type}: {message.Metadata}"
        );

        return new MessageResult(
            message.Id.ToString(),
            message.StreamName,
            message.StreamPosition,
            message.GlobalPosition,
            type,
            data,
            metadata,
            message.Timestamp
        );
    }

    public (object Message, Dictionary<string, object> Metadata) Deserialize(PostgresRecurringMessage message)
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

    public (object Message, Dictionary<string, object> Metadata) Deserialize(PostgresScheduledMessage message)
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
