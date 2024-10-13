using Beckett.Database;
using Beckett.Subscriptions;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Dashboard.Subscriptions.Queries;

public class GetSubscription(
    string groupName,
    string name,
    PostgresOptions options) : IPostgresDatabaseQuery<GetSubscriptionResult?>
{
    public async Task<GetSubscriptionResult?> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        command.CommandText = $@"
            SELECT group_name, name, status
            FROM {options.Schema}.subscriptions
            WHERE group_name = $1
            AND name = $2;
        ";

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

        if (!reader.HasRows)
        {
            return null;
        }

        return new GetSubscriptionResult(
            reader.GetFieldValue<string>(0),
            reader.GetFieldValue<string>(1),
            reader.GetFieldValue<SubscriptionStatus>(2)
        );
    }
}