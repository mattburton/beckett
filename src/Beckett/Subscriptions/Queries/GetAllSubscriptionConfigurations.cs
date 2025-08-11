using Beckett.Database;
using Npgsql;

namespace Beckett.Subscriptions.Queries;

public class GetAllSubscriptionConfigurations : IPostgresDatabaseQuery<IReadOnlyList<GetAllSubscriptionConfigurations.Result>>
{
    public async Task<IReadOnlyList<Result>> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        //language=sql
        const string sql = """
            SELECT s.id,
                   sg.name as group_name,
                   s.name as subscription_name,
                   s.category,
                   s.stream_name,
                   s.message_types,
                   s.priority,
                   s.skip_during_replay
            FROM beckett.subscriptions s
            INNER JOIN beckett.subscription_groups sg ON s.subscription_group_id = sg.id
            WHERE s.status IN ('active', 'replay')
            ORDER BY sg.name, s.priority, s.name;
        """;

        command.CommandText = Query.Build(nameof(GetAllSubscriptionConfigurations), sql, out var prepare);

        if (prepare)
        {
            await command.PrepareAsync(cancellationToken);
        }

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<Result>();

        while (await reader.ReadAsync(cancellationToken))
        {
            var messageTypes = reader.IsDBNull(5) ? new string[0] : reader.GetFieldValue<string[]>(5);

            results.Add(
                new Result(
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

    public record Result(
        long SubscriptionId,
        string GroupName,
        string SubscriptionName,
        string? Category,
        string? StreamName,
        string[] MessageTypes,
        int Priority,
        bool SkipDuringReplay
    );
}