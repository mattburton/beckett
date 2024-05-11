using Npgsql;

namespace Beckett.Database.Models;

public class PostgresMessage
{
    public Guid Id { get; init; }
    public string Topic { get; init; } = null!;
    public string StreamId { get; init; } = null!;
    public long StreamPosition { get; init; }
    public long GlobalPosition { get; init; }
    public string Type { get; init; } = null!;
    public string Data { get; init; } = null!;
    public string Metadata { get; init; } = null!;
    public DateTimeOffset Timestamp { get; init; }

    public static PostgresMessage From(NpgsqlDataReader reader)
    {
        return new PostgresMessage
        {
            Id = reader.GetFieldValue<Guid>(0),
            Topic = reader.GetFieldValue<string>(1),
            StreamId = reader.GetFieldValue<string>(2),
            StreamPosition = reader.GetFieldValue<int>(3),
            GlobalPosition = reader.GetFieldValue<int>(4),
            Type = reader.GetFieldValue<string>(5),
            Data = reader.GetFieldValue<string>(6),
            Metadata = reader.GetFieldValue<string>(7),
            Timestamp = reader.GetFieldValue<DateTimeOffset>(8)
        };
    }
}
