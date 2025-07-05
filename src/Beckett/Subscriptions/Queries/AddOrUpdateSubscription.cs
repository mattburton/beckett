using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Subscriptions.Queries;

public class AddOrUpdateSubscription(
    string groupName,
    string name
) : IPostgresDatabaseQuery<SubscriptionStatus>
{
    public async Task<SubscriptionStatus> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        //language=sql
        const string sql = """
            WITH insert_subscription AS (
                INSERT INTO beckett.subscriptions (group_name, name)
                VALUES ($1, $2)
                ON CONFLICT (group_name, name) DO NOTHING
                RETURNING status
            )
            SELECT status
            FROM insert_subscription
            UNION
            SELECT status
            FROM beckett.subscriptions
            WHERE group_name = $1
            AND name = $2;
        """;

        command.CommandText = Query.Build(nameof(AddOrUpdateSubscription), sql, out var prepare);

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });

        if (prepare)
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
