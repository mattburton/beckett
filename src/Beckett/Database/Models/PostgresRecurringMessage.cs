using System.Text.Json;
using Beckett.Messages;
using Npgsql;

namespace Beckett.Database.Models;

public class PostgresRecurringMessage
{
    public required string Name { get; init; }
    public required string CronExpression { get; init; }
    public required string TimeZoneId { get; init; }
    public required string StreamName { get; init; }
    public required string Type { get; init; }
    public required JsonElement Data { get; init; }
    public required JsonElement Metadata { get; init; }
    public DateTimeOffset? NextOccurrence { get; init; }
    public required DateTimeOffset Timestamp { get; init; }

    public static PostgresRecurringMessage From(NpgsqlDataReader reader)
    {
        using var data = reader.GetFieldValue<JsonDocument>(5);
        using var metadata = reader.GetFieldValue<JsonDocument>(6);

        return new PostgresRecurringMessage
        {
            Name = reader.GetFieldValue<string>(0),
            CronExpression = reader.GetFieldValue<string>(1),
            TimeZoneId = reader.GetFieldValue<string>(2),
            StreamName = reader.GetFieldValue<string>(3),
            Type = reader.GetFieldValue<string>(4),
            Data = data.RootElement.Clone(),
            Metadata = metadata.RootElement.Clone(),
            NextOccurrence = reader.IsDBNull(7) ? null : reader.GetFieldValue<DateTimeOffset?>(7),
            Timestamp = reader.GetFieldValue<DateTimeOffset>(8)
        };
    }

    public Message ToMessage() => new(Type, Data, Metadata.ToMetadataDictionary());
}
