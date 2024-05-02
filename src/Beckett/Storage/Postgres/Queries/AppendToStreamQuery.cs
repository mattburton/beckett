using Beckett.Storage.Postgres.Types;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Storage.Postgres.Queries;

internal static class AppendToStreamQuery
{
    public static async Task<long> Execute(
        NpgsqlConnection connection,
        string schema,
        string streamName,
        long expectedVersion,
        NewStreamEvent[] events,
        bool sendPollingNotification,
        CancellationToken cancellationToken
    )
    {
        await using var command = connection.CreateCommand();

        command.CommandText = $"select {schema}.append_to_stream($1, $2, $3, $4);";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint });
        command.Parameters.Add(new NpgsqlParameter { DataTypeName = NewStreamEvent.DataTypeNameFor(schema) });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Boolean });

        await command.PrepareAsync(cancellationToken);

        command.Parameters[0].Value = streamName;
        command.Parameters[1].Value = expectedVersion;
        command.Parameters[2].Value = events;
        command.Parameters[3].Value = sendPollingNotification;

        var result = await command.ExecuteScalarAsync(cancellationToken);

        if (result is long streamVersion)
        {
            return streamVersion;
        }

        throw new InvalidOperationException($"Unexpected result from append_to_stream function: {result}");
    }
}