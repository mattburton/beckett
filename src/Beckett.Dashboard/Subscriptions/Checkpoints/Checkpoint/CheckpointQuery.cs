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
                   sg.name as group_name,
                   s.name,
                   c.stream_name,
                   COALESCE(cr.target_stream_version, cres.target_stream_version, c.stream_position) as target_stream_version,
                   c.stream_position,
                   c.status,
                   cr.process_at,
                   cres.reserved_until,
                   c.retries,
                   m.stream_name as actual_stream_name,
                   m.stream_position as actual_stream_position
            FROM beckett.checkpoints c
            INNER JOIN beckett.subscriptions s ON c.subscription_id = s.id
            INNER JOIN beckett.subscription_groups sg ON s.subscription_group_id = sg.id
            LEFT JOIN beckett.checkpoints_ready cr ON c.id = cr.id
            LEFT JOIN beckett.checkpoints_reserved cres ON c.id = cres.id
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
            TargetStreamVersion = reader.GetFieldValue<long>(4),
            StreamPosition = reader.GetFieldValue<long>(5),
            Status = reader.GetFieldValue<CheckpointStatus>(6),
            ProcessAt = reader.IsDBNull(7) ? null : reader.GetFieldValue<DateTimeOffset>(7),
            ReservedUntil = reader.IsDBNull(8) ? null : reader.GetFieldValue<DateTimeOffset>(8),
            Retries = reader.IsDBNull(9) ? [] : reader.GetFieldValue<RetryType[]>(9),
            ActualStreamName = reader.IsDBNull(10) ? null : reader.GetFieldValue<string>(10),
            ActualStreamPosition = reader.IsDBNull(11) ? null : reader.GetFieldValue<long>(11)
        };
    }

    public class Result
    {
        public required long Id { get; init; }
        public required string GroupName { get; init; }
        public required string Name { get; init; }
        public required string StreamName { get; init; }
        public required long TargetStreamVersion { get; init; }
        public required long StreamPosition { get; init; }
        public required CheckpointStatus Status { get; init; }
        public DateTimeOffset? ProcessAt { get; init; }
        public DateTimeOffset? ReservedUntil { get; init; }
        public required RetryType[] Retries { get; init; }
        public required string? ActualStreamName { get; init; }
        public required long? ActualStreamPosition { get; init; }

        public int TotalAttempts => Retries?.Length > 0 ? Retries.Length - 1 : 0;

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
