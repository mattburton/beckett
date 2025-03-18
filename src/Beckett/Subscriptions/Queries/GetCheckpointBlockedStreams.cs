using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Subscriptions.Queries;

public class GetCheckpointBlockedStreams(
    long checkpointId,
    string[] streamNames,
    PostgresOptions options
) : IPostgresDatabaseQuery<IReadOnlyList<string>>
{
    public async Task<IReadOnlyList<string>> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        command.CommandText = $"select stream_name from {options.Schema}.get_checkpoint_blocked_streams($1, $2);";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Text });

        if (options.PrepareStatements)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = checkpointId;
        command.Parameters[1].Value = streamNames;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var streams = new List<string>();

        while (await reader.ReadAsync(cancellationToken))
        {
            streams.Add(reader.GetFieldValue<string>(0));
        }

        return streams;
    }
}
