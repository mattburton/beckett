using Npgsql;

namespace Beckett.Database.Models;

public class PostgresScheduledMessage
{
    public required string Application { get; init; }
    public required Guid Id { get; init; }
    public required string StreamName { get; init; }
    public required string Type { get; init; }
    public required string Data { get; init; }
    public required string Metadata { get; init; }
    public required DateTimeOffset DeliverAt { get; init; }
    public required DateTimeOffset Timestamp { get; init; }

    public static PostgresScheduledMessage From(NpgsqlDataReader reader) =>
        new()
        {
            Application = reader.GetFieldValue<string>(0),
            Id = reader.GetFieldValue<Guid>(1),
            StreamName = reader.GetFieldValue<string>(2),
            Type = reader.GetFieldValue<string>(3),
            Data = reader.GetFieldValue<string>(4),
            Metadata = reader.GetFieldValue<string>(5),
            DeliverAt = reader.GetFieldValue<DateTimeOffset>(6),
            Timestamp = reader.GetFieldValue<DateTimeOffset>(7)
        };
}
