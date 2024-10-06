using Beckett.Database;
using Npgsql;

namespace Beckett.Dashboard.Metrics.Queries;

public class GetSubscriptionRetryCount(PostgresOptions options) : IPostgresDatabaseQuery<long>
{
    public async Task<long> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        command.CommandText = $@"
            SELECT count(*)
            FROM {options.Schema}.checkpoints
            WHERE status = 'retry';
        ";

        if (options.PrepareStatements)
        {
            await command.PrepareAsync(cancellationToken);
        }

        return (long)(await command.ExecuteScalarAsync(cancellationToken))!;
    }
}
