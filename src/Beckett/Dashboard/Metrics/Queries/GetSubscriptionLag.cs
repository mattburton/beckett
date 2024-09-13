using Beckett.Database;
using Npgsql;

namespace Beckett.Dashboard.Metrics.Queries;

public class GetSubscriptionLag : IPostgresDatabaseQuery<long>
{
    public async Task<long> Execute(
        NpgsqlCommand command,
        string schema,
        CancellationToken cancellationToken
    )
    {
        command.CommandText = $@"
            SELECT count(*)
            FROM {schema}.checkpoints c
            INNER JOIN {schema}.subscriptions s ON c.group_name = s.group_name AND c.name = s.name
            WHERE c.status = 'lagging'
            AND s.status = 'active';
        ";

        await command.PrepareAsync(cancellationToken);

        return (long)(await command.ExecuteScalarAsync(cancellationToken))!;
    }
}
