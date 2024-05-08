using Npgsql;
using NpgsqlTypes;

namespace Beckett.Database.Queries;

public class SetSubscriptionToInitialized(string name) : IPostgresDatabaseQuery<int>
{
    public async Task<int> Execute(NpgsqlCommand command, string schema, CancellationToken cancellationToken)
    {
        command.CommandText = $"select {schema}.set_subscription_to_initialized($1);";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });

        await command.PrepareAsync(cancellationToken);

        command.Parameters[0].Value = name;

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
