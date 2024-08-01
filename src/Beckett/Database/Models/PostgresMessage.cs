using Npgsql;

namespace Beckett.Database.Models;

public class PostgresMessage
{
    public required Guid Id { get; init; }
    public required string StreamName { get; init; }
    public required long StreamVersion { get; init; }
    public required long StreamPosition { get; init; }
    public required long GlobalPosition { get; init; }
    public required string Type { get; init; }
    public required string Data { get; init; }
    public required string Metadata { get; init; }
    public required DateTimeOffset Timestamp { get; init; }

    public static PostgresMessage From(NpgsqlDataReader reader) =>
        new()
        {
            Id = reader.GetFieldValue<Guid>(0),
            StreamName = reader.GetFieldValue<string>(1),
            StreamVersion = reader.GetFieldValue<long>(2),
            StreamPosition = reader.GetFieldValue<long>(3),
            GlobalPosition = reader.GetFieldValue<long>(4),
            Type = reader.GetFieldValue<string>(5),
            Data = reader.GetFieldValue<string>(6),
            Metadata = reader.GetFieldValue<string>(7),
            Timestamp = reader.GetFieldValue<DateTimeOffset>(8)
        };
}
