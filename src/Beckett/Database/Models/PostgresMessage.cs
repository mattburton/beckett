using Npgsql;

namespace Beckett.Database.Models;

public class PostgresMessage
{
    public Guid Id { get; init; }
    public string StreamName { get; init; } = null!;
    public long StreamVersion { get; init; }
    public long StreamPosition { get; init; }
    public long GlobalPosition { get; init; }
    public string Type { get; init; } = null!;
    public string Data { get; init; } = null!;
    public string Metadata { get; init; } = null!;
    public DateTimeOffset Timestamp { get; init; }

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
