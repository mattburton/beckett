using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Subscriptions.Queries;

public class GetSubscriptionCheckpointCount(
    string groupName,
    string name,
    PostgresOptions options
) : IPostgresDatabaseQuery<long>
{
    public async Task<long> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        command.CommandText = $@"
            SELECT count(*)
            FROM {options.Schema}.checkpoints
            WHERE group_name = $1
            AND name = $2
            AND stream_name != '$initializing';
        ";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });

        if (options.PrepareStatements)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = groupName;
        command.Parameters[1].Value = name;

        var result = await command.ExecuteScalarAsync(cancellationToken);

        return result switch
        {
            long count => count,
            _ => throw new Exception($"Unexpected result from subscription checkpoint count: {result}")
        };
    }
}
