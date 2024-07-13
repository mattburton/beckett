using Npgsql;

namespace Beckett.Database.Models;

public class PostgresRecurringMessage
{
    public required string Name { get; init; }
    public required string CronExpression { get; init; }
    public required string StreamName { get; init; }
    public required string Type { get; init; }
    public required string Data { get; init; }
    public required string Metadata { get; init; }
    public DateTimeOffset? NextOccurrence { get; init; }
    public required DateTimeOffset Timestamp { get; init; }

    public static PostgresRecurringMessage From(NpgsqlDataReader reader) =>
        new()
        {
            Name = reader.GetFieldValue<string>(0),
            CronExpression = reader.GetFieldValue<string>(1),
            StreamName = reader.GetFieldValue<string>(2),
            Type = reader.GetFieldValue<string>(3),
            Data = reader.GetFieldValue<string>(4),
            Metadata = reader.GetFieldValue<string>(5),
            NextOccurrence = reader.IsDBNull(6) ? null : reader.GetFieldValue<DateTimeOffset?>(6),
            Timestamp = reader.GetFieldValue<DateTimeOffset>(7)
        };
}
