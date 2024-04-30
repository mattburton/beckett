using Beckett.Subscriptions;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Storage.Postgres.Queries;

public static class GetSubscriptionStreamsToProcessQuery
{
    public static async Task<IReadOnlyList<SubscriptionStream>> Execute(
        NpgsqlConnection connection,
        int batchSize,
        CancellationToken cancellationToken
    )
    {
        const string sql = "select subscription_name, stream_name from get_subscription_streams_to_process($1);";

        await using var command = connection.CreateCommand();

        command.CommandText = sql;

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer });

        await command.PrepareAsync(cancellationToken);

        command.Parameters[0].Value = batchSize;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<SubscriptionStream>();

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new SubscriptionStream(reader.GetFieldValue<string>(0), reader.GetFieldValue<string>(1)));
        }

        return results;
    }
}
