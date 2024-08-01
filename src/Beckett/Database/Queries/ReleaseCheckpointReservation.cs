using Npgsql;
using NpgsqlTypes;

namespace Beckett.Database.Queries;

public class ReleaseCheckpointReservation(long id) : IPostgresDatabaseQuery<int>
{
    public async Task<int> Execute(NpgsqlCommand command, string schema, CancellationToken cancellationToken)
    {
        command.CommandText = $@"
            select {schema}.release_checkpoint_reservation($1);
        ";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint });

        await command.PrepareAsync(cancellationToken);

        command.Parameters[0].Value = id;

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
