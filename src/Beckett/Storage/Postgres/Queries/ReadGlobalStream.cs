using Beckett.Database;
using Beckett.Database.Types;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Storage.Postgres.Queries;

public class ReadGlobalStream(ReadGlobalStreamOptions readOptions, PostgresOptions options)
    : IPostgresDatabaseQuery<ReadGlobalStream.Result>
{
    public async Task<Result> Execute(
        NpgsqlCommand command,
        CancellationToken cancellationToken
    )
    {
        command.CommandText = $@"
            select messages, ending_global_position
            from {options.Schema}.read_global_stream($1, $2, $3, $4);
        ";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Bigint });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text, IsNullable = true });
        command.Parameters.Add(
            new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Text, IsNullable = true }
        );

        if (options.PrepareStatements)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = readOptions.StartingGlobalPosition;
        command.Parameters[1].Value = readOptions.Count;
        command.Parameters[2].Value =
            string.IsNullOrWhiteSpace(readOptions.Category) ? DBNull.Value : readOptions.Category;
        command.Parameters[3].Value = readOptions.Types is { Length: > 0 } ? readOptions.Types : DBNull.Value;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        await reader.ReadAsync(cancellationToken);

        var messages = reader.GetFieldValue<StreamMessageType[]>(0);
        var globalPosition = reader.GetFieldValue<long>(1);

        return new Result(messages, globalPosition);
    }

    public record Result(IReadOnlyList<StreamMessageType> Messages, long EndingGlobalPosition);
}
