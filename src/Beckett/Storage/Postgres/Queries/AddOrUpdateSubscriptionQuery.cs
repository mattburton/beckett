using Npgsql;
using NpgsqlTypes;

namespace Beckett.Storage.Postgres.Queries;

public static class AddOrUpdateSubscriptionQuery
{
    public static async Task Execute(
        NpgsqlConnection connection,
        string schema,
        string subscriptionName,
        string[] eventTypes,
        bool startFromBeginning,
        CancellationToken cancellationToken
    )
    {
        await using var command = connection.CreateCommand();

        command.CommandText = $"select {schema}.add_or_update_subscription($1, $2, $3);";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Boolean });

        await command.PrepareAsync(cancellationToken);

        command.Parameters[0].Value = subscriptionName;
        command.Parameters[1].Value = eventTypes;
        command.Parameters[2].Value = startFromBeginning;

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
