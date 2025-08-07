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
            const string sql = """
                WITH current_version AS (
                    SELECT coalesce(max(m.stream_position), 0) AS version
                    FROM beckett.messages m
                    WHERE m.stream_name = $1
                    AND m.archived = false
                ),
                validate_expected_version AS (
                    SELECT
                        cv.version,
                        beckett.assert_condition(
                            $2 >= -2,
                            format('Invalid value for expected version: %s', $2)
                        )
                        AND beckett.assert_condition(
                            NOT ($2 = -1 AND cv.version = 0),
                            format('Attempted to append to a non-existing stream: %s', $1)
                        )
                        AND beckett.assert_condition(
                            NOT ($2 = 0 AND cv.version > 0),
                            format('Attempted to start a stream that already exists: %s', $1)
                        )
                        AND beckett.assert_condition(
                            NOT ($2 > 0 AND $2 != cv.version),
                            format('Stream %s version %s does not match expected version %s', $1, cv.version, $2)
                        ) AS valid
                    FROM current_version cv
                ),
                append_messages AS (
                    INSERT INTO beckett.messages (
                        id,
                        stream_position,
                        stream_name,
                        type,
                        data,
                        metadata
                    )
                    SELECT m.id,
                        v.version + (row_number() over())::bigint,
                        $1,
                        m.type,
                        m.data,
                        m.metadata
                    FROM unnest($3) AS m
                    CROSS JOIN validate_expected_version v
                    WHERE v.valid
                    RETURNING stream_position
                )
                SELECT max(stream_position), pg_notify('beckett:messages', NULL)
                FROM append_messages;
            """;

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
