using Beckett.Database;
using Npgsql;

namespace Beckett.Dashboard.Metrics.Queries;

public class GetSubscriptionLagCount(PostgresOptions options) : IPostgresDatabaseQuery<long>
{
    public async Task<long> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        command.CommandText = $"select {options.Schema}.get_subscription_lag_count();";

        if (options.PrepareStatements)
        {
            await command.PrepareAsync(cancellationToken);
        }

        return (long)(await command.ExecuteScalarAsync(cancellationToken))!;
    }
}
