using Npgsql;
using NpgsqlTypes;

namespace Beckett.Database.Queries;

public class GetSubscriptionFailedCount(string application) : IPostgresDatabaseQuery<long>
{
    public async Task<long> Execute(
        NpgsqlCommand command,
        string schema,
        CancellationToken cancellationToken
    )
    {
        command.CommandText = $"select {schema}.get_subscription_failed_count($1);";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });

        await command.PrepareAsync(cancellationToken);

        command.Parameters[0].Value = application;

        return (long)(await command.ExecuteScalarAsync(cancellationToken))!;
    }
}
