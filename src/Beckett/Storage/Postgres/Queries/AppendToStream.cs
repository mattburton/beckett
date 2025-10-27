using Beckett.Database;
using Beckett.Database.Types;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Storage.Postgres.Queries;

public class AppendToStream(
    string streamName,
    long expectedVersion,
    MessageType[] messages
) : IPostgresDatabaseQuery<long>
{
    public async Task<long> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        try
        {
            //language=sql
            const string sql = "select beckett.append_to_stream($1, $2, $3);";

            command.CommandText = Query.Build(nameof(AppendToStream), sql, out var prepare);

            command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
            command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint });
            command.Parameters.Add(new NpgsqlParameter { DataTypeName = DataTypeNames.MessageArray() });

            if (prepare)
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
