using Beckett.Database;
using Npgsql;

namespace Beckett.Dashboard.Metrics.Queries;

public class GetSubscriptionRetryCount() : IPostgresDatabaseQuery<long>
{
    public async Task<long> Execute(
        NpgsqlCommand command,
        string schema,
        CancellationToken cancellationToken
    )
    {
        command.CommandText = $"select {schema}.get_subscription_retry_count();";

        await command.PrepareAsync(cancellationToken);

        return (long)(await command.ExecuteScalarAsync(cancellationToken))!;
    }
}
