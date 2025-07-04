using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Subscriptions.Queries;

public class SetSubscriptionToActive(
    string groupName,
    string name,
    PostgresOptions options
) : IPostgresDatabaseQuery<int>
{
    public async Task<int> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        command.CommandText = $"""
            WITH delete_initialization_checkpoint AS (
                DELETE FROM {options.Schema}.checkpoints
                WHERE group_name = $1
                AND name = $2
                AND stream_name = '$initializing'
            )
            UPDATE {options.Schema}.subscriptions
            SET status = 'active',
                replay_target_position = NULL
            WHERE group_name = $1
            AND name = $2;
        """;

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });

        if (options.PrepareStatements)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = groupName;
        command.Parameters[1].Value = name;

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
