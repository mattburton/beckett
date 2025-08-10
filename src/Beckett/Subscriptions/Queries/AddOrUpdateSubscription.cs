using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Subscriptions.Queries;

public class AddOrUpdateSubscription(
    string groupName,
    string name
) : IPostgresDatabaseQuery<(long SubscriptionId, SubscriptionStatus Status)>
{
    public async Task<(long SubscriptionId, SubscriptionStatus Status)> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        //language=sql
        const string sql = """
            WITH ensure_group AS (
                INSERT INTO beckett.subscription_groups (name)
                VALUES ($1)
                ON CONFLICT (name) DO NOTHING
                RETURNING id
            ),
            subscription_group_id AS (
                SELECT id FROM ensure_group
                UNION ALL
                SELECT id FROM beckett.subscription_groups WHERE name = $1
                LIMIT 1
            ),
            insert_subscription AS (
                INSERT INTO beckett.subscriptions (subscription_group_id, name, status)
                SELECT g.id, $2, 
                       CASE WHEN $2 = '$global' THEN 'active'::beckett.subscription_status 
                            ELSE 'uninitialized'::beckett.subscription_status END
                FROM subscription_group_id g
                ON CONFLICT (subscription_group_id, name) DO NOTHING
                RETURNING id, status
            )
            SELECT id, status
            FROM insert_subscription
            UNION ALL
            SELECT s.id, s.status
            FROM beckett.subscriptions s
            INNER JOIN beckett.subscription_groups sg ON s.subscription_group_id = sg.id
            WHERE sg.name = $1 AND s.name = $2;
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

        return (reader.GetFieldValue<long>(0), reader.GetFieldValue<SubscriptionStatus>(1));
    }
}
