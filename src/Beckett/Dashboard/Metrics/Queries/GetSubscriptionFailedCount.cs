using Beckett.Database;
using Npgsql;

namespace Beckett.Dashboard.Metrics.Queries;

public class GetSubscriptionFailedCount : IPostgresDatabaseQuery<long>
{
    public async Task<long> Execute(
        NpgsqlCommand command,
        string schema,
        CancellationToken cancellationToken
    )
    {
        command.CommandText = $@"
            SELECT count(*)
            FROM {schema}.checkpoints
            WHERE status = 'failed';
        ";

        await command.PrepareAsync(cancellationToken);

        return (long)(await command.ExecuteScalarAsync(cancellationToken))!;
    }
}
