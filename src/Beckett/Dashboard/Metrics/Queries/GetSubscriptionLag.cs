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
        command.CommandText = $"select {schema}.get_subscription_lag();";

        await command.PrepareAsync(cancellationToken);

        return (long)(await command.ExecuteScalarAsync(cancellationToken))!;
    }
}
