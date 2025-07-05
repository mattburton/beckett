using Beckett.Database;
using Beckett.Database.Types;
using Npgsql;

namespace Beckett.Subscriptions.Queries;

public class RecordCheckpoints(CheckpointType[] checkpoints) : IPostgresDatabaseQuery<int>
{
    public async Task<int> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO beckett.checkpoints (stream_version, stream_position, group_name, name, stream_name)
            SELECT c.stream_version, c.stream_position, c.group_name, c.name, c.stream_name
            FROM unnest($1) c
            ON CONFLICT (group_name, name, stream_name) DO UPDATE
                SET stream_version = excluded.stream_version;
        """;

        command.CommandText = Query.Build(nameof(RecordCheckpoints), sql, out var prepare);

        command.Parameters.Add(new NpgsqlParameter { DataTypeName = DataTypeNames.CheckpointArray("beckett") });

        if (prepare)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = checkpoints;

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
