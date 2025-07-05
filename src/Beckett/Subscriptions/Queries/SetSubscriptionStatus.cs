using Beckett.Database;
using Beckett.Database.Types;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Subscriptions.Queries;

public class SetSubscriptionStatus(
    string groupName,
    string name,
    SubscriptionStatus status
) : IPostgresDatabaseQuery<int>
{
    public async Task<int> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE beckett.subscriptions
            SET status = $3
            WHERE group_name = $1
            AND name = $2;
        """;

        command.CommandText = Query.Build(nameof(SetSubscriptionStatus), sql, out var prepare);

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { DataTypeName = DataTypeNames.SubscriptionStatus("beckett") });

        if (prepare)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = groupName;
        command.Parameters[1].Value = name;
        command.Parameters[2].Value = status;

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
