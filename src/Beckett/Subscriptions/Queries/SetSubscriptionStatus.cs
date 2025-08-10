using Beckett.Database;
using Beckett.Database.Types;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Subscriptions.Queries;

public class SetSubscriptionStatus(
    long subscriptionId,
    SubscriptionStatus status
) : IPostgresDatabaseQuery<int>
{
    public async Task<int> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        //language=sql
        const string sql = """
            UPDATE beckett.subscriptions
            SET status = $2
            WHERE id = $1;
        """;

        command.CommandText = Query.Build(nameof(SetSubscriptionStatus), sql, out var prepare);

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint });
        command.Parameters.Add(new NpgsqlParameter { DataTypeName = DataTypeNames.SubscriptionStatus() });

        if (prepare)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = subscriptionId;
        command.Parameters[1].Value = status;

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
