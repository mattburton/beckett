using Beckett.Storage.Postgres.Types;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Storage.Postgres.Queries;

public static class AppendToStreamQuery
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
        try
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

            return result switch
            {
                long streamVersion => streamVersion,
                DBNull => -1,
                _ => throw new InvalidOperationException($"Unexpected result from append_to_stream function: {result}")
            };
        }
        catch (PostgresException e)
        {
            const string streamDoesNotExistText = "non-existing stream";
            const string streamAlreadyExistsText = "stream that already exists";
            const string expectedVersionText = "expected version";

            if (e.MessageText.Contains(streamDoesNotExistText))
            {
                throw new StreamDoesNotExistException(e.MessageText);
            }

            if (e.MessageText.Contains(streamAlreadyExistsText))
            {
                throw new StreamAlreadyExistsException(e.MessageText);
            }

            if (e.MessageText.Contains(expectedVersionText))
            {
                throw new OptimisticConcurrencyException(e.MessageText);
            }

            throw;
        }
    }
}
