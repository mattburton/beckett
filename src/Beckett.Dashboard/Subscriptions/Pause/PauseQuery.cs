using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Dashboard.Subscriptions.Pause;

public class PauseQuery(
    string groupName,
    string name
) : IPostgresDatabaseQuery<int>
{
    public async Task<int> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        //language=sql
        const string sql = """
            UPDATE beckett.subscriptions
            SET status = 'paused'
            WHERE group_name = $1
            AND name = $2;
        """;

        command.CommandText = Query.Build(nameof(PauseQuery), sql, out var prepare);

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });

        if (prepare)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = groupName;
        command.Parameters[1].Value = name;

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
