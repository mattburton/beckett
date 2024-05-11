using Npgsql;

namespace Beckett.Database.Models;

public class PostgresScheduledMessage
{
    public Guid Id { get; init; }
    public string Topic { get; init; } = null!;
    public string StreamId { get; init; } = null!;
    public string Type { get; init; } = null!;
    public string Data { get; init; } = null!;
    public string Metadata { get; init; } = null!;
    public DateTimeOffset DeliverAt { get; init; }
    public DateTimeOffset Timestamp { get; init; }

    public static PostgresScheduledMessage From(NpgsqlDataReader reader)
    {
        return new PostgresScheduledMessage
        {
            Id = reader.GetFieldValue<Guid>(0),
            Topic = reader.GetFieldValue<string>(1),
            StreamId = reader.GetFieldValue<string>(2),
            Type = reader.GetFieldValue<string>(3),
            Data = reader.GetFieldValue<string>(4),
            Metadata = reader.GetFieldValue<string>(5),
            DeliverAt = reader.GetFieldValue<DateTimeOffset>(6),
            Timestamp = reader.GetFieldValue<DateTimeOffset>(7)
        };
    }
}
