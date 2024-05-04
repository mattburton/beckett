using Npgsql;
using NpgsqlTypes;

namespace Beckett.Database.Queries;

public class AddOrUpdateSubscription(string name) : IPostgresDatabaseQuery<bool>
{
    public async Task<bool> Execute(NpgsqlCommand command, string schema, CancellationToken cancellationToken)
    {
        command.CommandText = $"select {schema}.add_or_update_subscription($1);";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });

        await command.PrepareAsync(cancellationToken);

        command.Parameters[0].Value = name;

        var result = await command.ExecuteScalarAsync(cancellationToken);

        return (bool)result!;
    }
}
