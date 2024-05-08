using Npgsql;

namespace Beckett.Database.Models;

public class PostgresScheduledEvent
{
    public Guid Id { get; init; }
    public string StreamName { get; init; } = null!;
    public string Type { get; init; } = null!;
    public string Data { get; init; } = null!;
    public string Metadata { get; init; } = null!;
    public DateTimeOffset DeliverAt { get; init; }
    public DateTimeOffset Timestamp { get; init; }

    public static PostgresScheduledEvent From(NpgsqlDataReader reader)
    {
        return new PostgresScheduledEvent
        {
            Id = reader.GetFieldValue<Guid>(0),
            StreamName = reader.GetFieldValue<string>(1),
            Type = reader.GetFieldValue<string>(2),
            Data = reader.GetFieldValue<string>(3),
            Metadata = reader.GetFieldValue<string>(4),
            DeliverAt = reader.GetFieldValue<DateTimeOffset>(5),
            Timestamp = reader.GetFieldValue<DateTimeOffset>(6)
        };
    }
}