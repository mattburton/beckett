using Beckett.Storage.Postgres.Types;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Storage.Postgres.Queries;

public static class ReadSubscriptionStreamQuery
{
    public static async Task<IReadOnlyList<StreamEvent>> Execute(
        NpgsqlConnection connection,
        string subscriptionName,
        string streamName,
        int batchSize,
        CancellationToken cancellationToken
    )
    {
        const string sql = @"
            select id,
                   stream_name,
                   stream_position,
                   global_position,
                   type,
                   data,
                   metadata,
                   timestamp
            from read_subscription_stream($1, $2, $3);
        ";

        await using var command = connection.CreateCommand();

        command.CommandText = sql;

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer });

        await command.PrepareAsync(cancellationToken);

        command.Parameters[0].Value = subscriptionName;
        command.Parameters[1].Value = streamName;
        command.Parameters[2].Value = batchSize;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<StreamEvent>();

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(StreamEvent.From(reader));
        }

        return results;
    }
}
