using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Subscriptions.Queries;

public class ReserveCheckpoint(
    long id,
    TimeSpan reservationTimeout,
    PostgresOptions options
) : IPostgresDatabaseQuery<long?>
{
    public async Task<long?> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        command.CommandText = $"select {options.Schema}.reserve_checkpoint($1, $2);";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Interval });

        if (options.PrepareStatements)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = id;
        command.Parameters[1].Value = reservationTimeout;

        var result = await command.ExecuteScalarAsync(cancellationToken);

        return result switch
        {
            long streamVersion => streamVersion,
            DBNull => null,
            _ => throw new Exception($"Unexpected result from reserve_checkpoint function: {result}")
        };
    }
}
