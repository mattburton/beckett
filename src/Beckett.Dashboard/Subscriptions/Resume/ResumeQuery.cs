using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Dashboard.Subscriptions.Resume;

public class ResumeQuery(
    string groupName,
    string name
) : IPostgresDatabaseQuery<int>
{
    public async Task<int> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        //language=sql
        const string sql = """
            UPDATE beckett.subscriptions s
            SET status = 'active'
            FROM beckett.subscription_groups sg
            WHERE s.subscription_group_id = sg.id
            AND sg.name = $1
            AND s.name = $2;
            
            SELECT pg_notify('beckett:checkpoints', s.id::text)
            FROM beckett.subscriptions s
            INNER JOIN beckett.subscription_groups sg ON s.subscription_group_id = sg.id
            WHERE sg.name = $1
            AND s.name = $2;
        """;

        command.CommandText = Query.Build(nameof(ResumeQuery), sql, out var prepare);

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
