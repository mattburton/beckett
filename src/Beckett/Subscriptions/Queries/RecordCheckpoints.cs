using Beckett.Database;
using Beckett.Database.Types;
using Npgsql;

namespace Beckett.Subscriptions.Queries;

public class RecordCheckpoints(CheckpointType[] checkpoints, PostgresOptions options) : IPostgresDatabaseQuery<int>
{
    public async Task<int> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        command.CommandText = $"""
            INSERT INTO {options.Schema}.checkpoints (stream_version, stream_position, group_name, name, stream_name)
            SELECT c.stream_version, c.stream_position, c.group_name, c.name, c.stream_name
            FROM unnest($1) c
            ON CONFLICT (group_name, name, stream_name) DO UPDATE
                SET stream_version = excluded.stream_version;
        """;

        command.Parameters.Add(new NpgsqlParameter { DataTypeName = DataTypeNames.CheckpointArray(options.Schema) });

        if (options.PrepareStatements)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = checkpoints;

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
