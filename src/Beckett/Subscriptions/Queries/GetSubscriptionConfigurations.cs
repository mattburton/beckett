using Beckett.Database;
using Npgsql;

namespace Beckett.Subscriptions.Queries;

public class GetSubscriptionConfigurations : IPostgresDatabaseQuery<IReadOnlyList<SubscriptionConfiguration>>
{
    public async Task<IReadOnlyList<SubscriptionConfiguration>> Execute(
        NpgsqlCommand command,
        CancellationToken cancellationToken
    )
    {
        //language=sql
        const string sql = """
            SELECT
                s.id,
                sg.name as group_name,
                s.name as subscription_name,
                s.category,
                s.stream_name,
                array_agg(mt.name ORDER BY mt.name) FILTER (WHERE mt.name IS NOT NULL) as message_types,
                s.priority,
                s.skip_during_replay
            FROM beckett.subscriptions s
            INNER JOIN beckett.subscription_groups sg ON s.subscription_group_id = sg.id
            LEFT JOIN beckett.subscription_message_types smt ON s.id = smt.subscription_id
            LEFT JOIN beckett.message_types mt ON smt.message_type_id = mt.id
            WHERE s.status IN ('active', 'replay')
            GROUP BY s.id, sg.name, s.name, s.category, s.stream_name, s.priority, s.skip_during_replay
            ORDER BY sg.name, s.priority, s.name;
            """;

        command.CommandText = Query.Build(nameof(GetSubscriptionConfigurations), sql, out var prepare);

        if (prepare)
        {
            await command.PrepareAsync(cancellationToken);
        }

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<SubscriptionConfiguration>();

        while (await reader.ReadAsync(cancellationToken))
        {
            var messageTypes = reader.IsDBNull(5) ? [] : reader.GetFieldValue<string[]>(5);

            results.Add(
                new SubscriptionConfiguration(
                    reader.GetFieldValue<long>(0),
                    reader.GetFieldValue<string>(1),
                    reader.GetFieldValue<string>(2),
                    reader.IsDBNull(3) ? null : reader.GetFieldValue<string>(3),
                    reader.IsDBNull(4) ? null : reader.GetFieldValue<string>(4),
                    messageTypes,
                    reader.GetFieldValue<int>(6),
                    reader.GetFieldValue<bool>(7)
                )
            );
        }

        return results;
    }
}
