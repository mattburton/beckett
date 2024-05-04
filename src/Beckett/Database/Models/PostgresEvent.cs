using Npgsql;

namespace Beckett.Database.Models;

public class PostgresEvent
{
    public Guid Id { get; init; }
    public string StreamName { get; init; } = null!;
    public long StreamPosition { get; init; }
    public long GlobalPosition { get; init; }
    public string Type { get; init; } = null!;
    public string Data { get; init; } = null!;
    public string Metadata { get; init; } = null!;
    public DateTimeOffset Timestamp { get; init; }

    public static PostgresEvent From(NpgsqlDataReader reader)
    {
        return new PostgresEvent
        {
            Id = reader.GetFieldValue<Guid>(0),
            StreamName = reader.GetFieldValue<string>(1),
            StreamPosition = reader.GetFieldValue<int>(2),
            GlobalPosition = reader.GetFieldValue<int>(3),
            Type = reader.GetFieldValue<string>(4),
            Data = reader.GetFieldValue<string>(5),
            Metadata = reader.GetFieldValue<string>(6),
            Timestamp = reader.GetFieldValue<DateTimeOffset>(7)
        };
    }
}
