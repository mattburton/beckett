using Beckett.Database;
using Npgsql;

namespace Beckett.Dashboard.Postgres.Metrics.Queries;

public class GetSubscriptionMetrics(PostgresOptions options) : IPostgresDatabaseQuery<GetSubscriptionMetricsResult>
{
    public async Task<GetSubscriptionMetricsResult> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        command.CommandText = $"select lagging, retries, failed from {options.Schema}.get_subscription_metrics();";

        if (options.PrepareStatements)
        {
            await command.PrepareAsync(cancellationToken);
        }

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        await reader.ReadAsync(cancellationToken);

        return new GetSubscriptionMetricsResult(
            reader.GetFieldValue<long>(0),
            reader.GetFieldValue<long>(1),
            reader.GetFieldValue<long>(2)
        );
    }
}
