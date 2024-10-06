using Beckett.Database;
using Beckett.Database.Types;
using Npgsql;

namespace Beckett.Subscriptions.Queries;

public class RecordCheckpoints(CheckpointType[] checkpoints, PostgresOptions options) : IPostgresDatabaseQuery<int>
{
    public async Task<int> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        command.CommandText = $"select {options.Schema}.record_checkpoints($1);";

        command.Parameters.Add(new NpgsqlParameter { DataTypeName = DataTypeNames.CheckpointArray(options.Schema) });

        if (options.PrepareStatements)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = checkpoints;

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
