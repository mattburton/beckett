using System.Text.Json;
using Beckett.Messages;
using Npgsql;

namespace Beckett.Database.Models;

public class PostgresScheduledMessage
{
    public required Guid Id { get; init; }
    public required string StreamName { get; init; }
    public required string Type { get; init; }
    public required JsonDocument Data { get; init; }
    public required JsonDocument Metadata { get; init; }
    public required DateTimeOffset DeliverAt { get; init; }
    public required DateTimeOffset Timestamp { get; init; }

    public static PostgresScheduledMessage From(NpgsqlDataReader reader) =>
        new()
        {
            Id = reader.GetFieldValue<Guid>(0),
            StreamName = reader.GetFieldValue<string>(1),
            Type = reader.GetFieldValue<string>(2),
            Data = reader.GetFieldValue<JsonDocument>(3),
            Metadata = reader.GetFieldValue<JsonDocument>(4),
            DeliverAt = reader.GetFieldValue<DateTimeOffset>(5),
            Timestamp = reader.GetFieldValue<DateTimeOffset>(6)
        };

    public Message ToMessage() => new(Type, Data, Metadata.ToMetadataDictionary());
}
