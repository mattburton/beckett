using Beckett.Database.Types;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Database.Queries;

public class AppendToStream(string streamName, long expectedVersion, EventType[] events) : IPostgresDatabaseQuery<long>
{
    public async Task<long> Execute(NpgsqlCommand command, string schema, CancellationToken cancellationToken)
    {
        try
        {
            command.CommandText = $"select {schema}.append_to_stream($1, $2, $3);";

            command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
            command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint });
            command.Parameters.Add(new NpgsqlParameter { DataTypeName = EventType.DataTypeNameFor(schema) });

            await command.PrepareAsync(cancellationToken);

            command.Parameters[0].Value = streamName;
            command.Parameters[1].Value = expectedVersion;
            command.Parameters[2].Value = events;

            var result = await command.ExecuteScalarAsync(cancellationToken);

            return result switch
            {
                long streamVersion => streamVersion,
                DBNull => -1,
                _ => throw new Exception($"Unexpected result from append_to_stream function: {result}")
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
