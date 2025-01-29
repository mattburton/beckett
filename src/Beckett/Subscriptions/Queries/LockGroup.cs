using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Subscriptions.Queries;

public class LockGroup(
    int id,
    PostgresOptions options
) : IPostgresDatabaseQuery<LockGroup.Result?>
{
    public async Task<Result?> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        command.CommandText = $"select id, global_position from {options.Schema}.lock_group($1);";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer });

        if (options.PrepareStatements)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = id;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        await reader.ReadAsync(cancellationToken);

        return !reader.HasRows ? null : new Result(reader.GetFieldValue<int>(0), reader.GetFieldValue<long>(1));
    }

    public record Result(int Id, long GlobalPosition);
}
