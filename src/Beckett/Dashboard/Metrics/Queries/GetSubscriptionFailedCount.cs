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
        command.CommandText = $"select {schema}.get_subscription_failed_count();";

        await command.PrepareAsync(cancellationToken);

        return (long)(await command.ExecuteScalarAsync(cancellationToken))!;
    }
}
