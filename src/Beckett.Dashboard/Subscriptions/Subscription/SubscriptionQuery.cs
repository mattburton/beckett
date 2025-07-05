using Beckett.Database;
using Beckett.Subscriptions;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Dashboard.Subscriptions.Subscription;

public class SubscriptionQuery(
    string groupName,
    string name) : IPostgresDatabaseQuery<SubscriptionQuery.Result?>
{
    public async Task<Result?> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT group_name, name, status
            FROM beckett.subscriptions
            WHERE group_name = $1
            AND name = $2;
        """;

        command.CommandText = Query.Build(nameof(SubscriptionQuery), sql, out var prepare);

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

        if (!reader.HasRows)
        {
            return null;
        }

        return new Result(
            reader.GetFieldValue<string>(0),
            reader.GetFieldValue<string>(1),
            reader.GetFieldValue<SubscriptionStatus>(2)
        );
    }

    public record Result(string GroupName, string Name, SubscriptionStatus Status);
}
