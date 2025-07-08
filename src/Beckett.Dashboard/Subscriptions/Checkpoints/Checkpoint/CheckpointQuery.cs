using Beckett.Database;
using Beckett.Database.Types;
using Beckett.Subscriptions;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Dashboard.Subscriptions.Checkpoints.Checkpoint;

public class CheckpointQuery(long id) : IPostgresDatabaseQuery<CheckpointQuery.Result?>
{
    public async Task<Result?> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        //language=sql
        const string sql = """
            SELECT c.id,
                   c.group_name,
                   c.name,
                   c.stream_name,
                   c.stream_position,
                   c.status,
                   r.process_at,
                   res.reserved_until,
                   c.retries,
                   m.stream_name as actual_stream_name,
                   m.stream_position as actual_stream_position
            FROM beckett.checkpoints c
            INNER JOIN beckett.checkpoints_ready r ON c.id = r.id
            INNER JOIN beckett.checkpoints_reserved res ON c.id = res.id
            LEFT JOIN beckett.messages m on c.stream_name = '$global' and c.stream_position = m.global_position
            WHERE c.id = $1;
        """;

        command.CommandText = Query.Build(nameof(CheckpointQuery), sql, out var prepare);

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint });

        if (prepare)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = id;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        await reader.ReadAsync(cancellationToken);

        if (!reader.HasRows)
        {
            return null;
        }

        return new Result{
            Id = reader.GetFieldValue<long>(0),
            GroupName = reader.GetFieldValue<string>(1),
            Name = reader.GetFieldValue<string>(2),
            StreamName = reader.GetFieldValue<string>(3),
            StreamPosition = reader.GetFieldValue<long>(4),
            Status = reader.GetFieldValue<CheckpointStatus>(5),
            ProcessAt = reader.IsDBNull(6) ? null : reader.GetFieldValue<DateTimeOffset>(6),
            ReservedUntil = reader.IsDBNull(7) ? null : reader.GetFieldValue<DateTimeOffset>(7),
            Retries = reader.IsDBNull(8) ? [] : reader.GetFieldValue<RetryType[]>(8),
            ActualStreamName = reader.IsDBNull(9) ? null : reader.GetFieldValue<string>(9),
            ActualStreamPosition = reader.IsDBNull(10) ? null : reader.GetFieldValue<long>(10)
        };
    }

    public class Result
    {
        public required long Id { get; init; }
        public required string GroupName { get; init; }
        public required string Name { get; init; }
        public required string StreamName { get; init; }
        public required long StreamPosition { get; init; }
        public required CheckpointStatus Status { get; init; }
        public DateTimeOffset? ProcessAt { get; init; }
        public DateTimeOffset? ReservedUntil { get; init; }
        public required RetryType[] Retries { get; init; }
        public required string? ActualStreamName { get; init; }
        public required long? ActualStreamPosition { get; init; }

        public int TotalAttempts => Retries.Length > 0 ? Retries.Length - 1 : 0;

        public string StreamCategory
        {
            get
            {
                var firstHyphen = StreamNameForLink.IndexOf('-');

                return firstHyphen < 0 ? StreamNameForLink : StreamNameForLink[..firstHyphen];
            }
        }

        public bool ShowControls => Status switch
        {
            CheckpointStatus.Active => false,
            _ => true
        };

        public string StreamNameForLink => ActualStreamName ?? StreamName;

        public long StreamPositionForLink => ActualStreamPosition ?? StreamPosition;
    }
}
