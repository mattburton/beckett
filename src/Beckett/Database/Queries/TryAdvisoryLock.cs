using Npgsql;
using NpgsqlTypes;

namespace Beckett.Database.Queries;

public class TryAdvisoryLock(string key, PostgresOptions options) : IPostgresDatabaseQuery<bool>
{
    public async Task<bool> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        command.CommandText = $"select {options.Schema}.try_advisory_lock($1);";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text, Value = key });

        var result = await command.ExecuteScalarAsync(cancellationToken);

        return result is not false;
    }
}
