using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Subscriptions.Queries;

public class GetSubscriptionCheckpointCount(
    string groupName,
    string name
) : IPostgresDatabaseQuery<long>
{
    public async Task<long> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        //language=sql
        const string sql = """
            SELECT count(*)
            FROM beckett.checkpoints
            WHERE group_name = $1
            AND name = $2
            AND stream_name != '$initializing';
        """;

        command.CommandText = Query.Build(nameof(GetSubscriptionCheckpointCount), sql, out var prepare);

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });

        if (prepare)
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
