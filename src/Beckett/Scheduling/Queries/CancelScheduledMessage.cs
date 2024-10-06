using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Scheduling.Queries;

public class CancelScheduledMessage(Guid id, PostgresOptions options) : IPostgresDatabaseQuery<int>
{
    public async Task<int> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        command.CommandText = $"select {options.Schema}.cancel_scheduled_message($1);";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Uuid });

        if (options.PrepareStatements)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = id;

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
