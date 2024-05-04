using Beckett.Database.Types;
using Npgsql;

namespace Beckett.Database.Queries;

public class RecordCheckpoints(CheckpointType[] checkpoints) : IPostgresDatabaseQuery<int>
{
    public async Task<int> Execute(NpgsqlCommand command, string schema, CancellationToken cancellationToken)
    {
        command.CommandText = $"select {schema}.record_checkpoints($1);";

        command.Parameters.Add(new NpgsqlParameter { DataTypeName = CheckpointType.DataTypeNameFor(schema) });

        await command.PrepareAsync(cancellationToken);

        command.Parameters[0].Value = checkpoints;

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
