using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Subscriptions.Queries;

public class AddOrUpdateSubscription(
    string groupName,
    string name,
    PostgresOptions options
) : IPostgresDatabaseQuery<SubscriptionStatus>
{
    public async Task<SubscriptionStatus> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        command.CommandText = $"select status from {options.Schema}.add_or_update_subscription($1, $2);";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });

        if (options.PrepareStatements)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = groupName;
        command.Parameters[1].Value = name;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        await reader.ReadAsync(cancellationToken);

        return reader.GetFieldValue<SubscriptionStatus>(0);
    }
}
