using Npgsql;
using NpgsqlTypes;

namespace Beckett.Database.Queries;

public class AdvisoryUnlock(string key, PostgresOptions options) : IPostgresDatabaseQuery<bool>
{
    public async Task<bool> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        command.CommandText = $"select {options.Schema}.advisory_unlock($1);";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text, Value = key });

        var result = await command.ExecuteScalarAsync(cancellationToken);

        return result is not false;
    }
}
