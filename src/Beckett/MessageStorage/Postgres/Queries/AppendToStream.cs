using Beckett.Database;
using Beckett.Database.Types;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.MessageStorage.Postgres.Queries;

public class AppendToStream(
    string streamName,
    long expectedVersion,
    MessageType[] messages,
    PostgresOptions options
) : IPostgresDatabaseQuery<long>
{
    public async Task<long> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        try
        {
            command.CommandText = $"select {options.Schema}.append_to_stream($1, $2, $3);";

            command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
            command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint });
            command.Parameters.Add(new NpgsqlParameter { DataTypeName = DataTypeNames.MessageArray(options.Schema) });

            if (options.PrepareStatements)
            {
                await command.PrepareAsync(cancellationToken);
            }

            command.Parameters[0].Value = streamName;
            command.Parameters[1].Value = expectedVersion;
            command.Parameters[2].Value = messages;

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
            e.HandleAppendToStreamError();

            throw;
        }
    }
}
