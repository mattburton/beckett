using System.Text.Json;
using Npgsql;

namespace Beckett.Database.Models;

public class PostgresStreamMessage
{
    public required Guid Id { get; init; }
    public required string StreamName { get; init; }
    public required long StreamPosition { get; init; }
    public required long GlobalPosition { get; init; }
    public required string Type { get; init; }
    public required JsonElement Data { get; init; }
    public required JsonElement Metadata { get; init; }
    public required DateTimeOffset Timestamp { get; init; }

    public static PostgresStreamMessage From(NpgsqlDataReader reader)
    {
        using var data = reader.GetFieldValue<JsonDocument>(6);
        using var metadata = reader.GetFieldValue<JsonDocument>(7);

        return new PostgresStreamMessage
        {
            Id = reader.GetFieldValue<Guid>(1),
            StreamName = reader.GetFieldValue<string>(2),
            StreamPosition = reader.GetFieldValue<long>(3),
            GlobalPosition = reader.GetFieldValue<long>(4),
            Type = reader.GetFieldValue<string>(5),
            Data = data.RootElement.Clone(),
            Metadata = metadata.RootElement.Clone(),
            Timestamp = reader.GetFieldValue<DateTimeOffset>(8)
        };
    }
}
