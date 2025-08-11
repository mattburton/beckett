using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Subscriptions.Queries;

public class UpsertSubscriptionConfiguration(
    string groupName,
    string subscriptionName,
    string? category,
    string? streamName,
    string[]? messageTypes,
    int priority,
    bool skipDuringReplay
) : IPostgresDatabaseQuery<int>
{
    public async Task<int> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        //language=sql
        const string sql = """
            UPDATE beckett.subscriptions
            SET category = $3,
                stream_name = $4,
                message_types = $5,
                priority = $6,
                skip_during_replay = $7
            FROM beckett.subscription_groups sg
            WHERE beckett.subscriptions.subscription_group_id = sg.id
            AND sg.name = $1
            AND beckett.subscriptions.name = $2;
        """;

        command.CommandText = Query.Build(nameof(UpsertSubscriptionConfiguration), sql, out var prepare);

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text, IsNullable = true });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text, IsNullable = true });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Text, IsNullable = true });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Boolean });

        if (prepare)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = groupName;
        command.Parameters[1].Value = subscriptionName;
        command.Parameters[2].Value = category ?? (object)DBNull.Value;
        command.Parameters[3].Value = streamName ?? (object)DBNull.Value;
        command.Parameters[4].Value = messageTypes ?? (object)DBNull.Value;
        command.Parameters[5].Value = priority;
        command.Parameters[6].Value = skipDuringReplay;

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }
}