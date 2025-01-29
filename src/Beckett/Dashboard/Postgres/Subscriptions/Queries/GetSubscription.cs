using Beckett.Database;
using Beckett.Subscriptions;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Dashboard.Postgres.Subscriptions.Queries;

public class GetSubscription(
    int id,
    PostgresOptions options) : IPostgresDatabaseQuery<GetSubscriptionResult?>
{
    public async Task<GetSubscriptionResult?> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        command.CommandText = $@"
            SELECT s.id,
                   g.name,
                   s.name,
                   s.status
            FROM {options.Schema}.subscriptions s
            INNER JOIN {options.Schema}.groups g ON s.group_id = g.id
            WHERE s.id = $1;
        ";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer });

        if (options.PrepareStatements)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = id;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        await reader.ReadAsync(cancellationToken);

        if (!reader.HasRows)
        {
            return null;
        }

        return new GetSubscriptionResult(
            reader.GetFieldValue<int>(0),
            reader.GetFieldValue<string>(1),
            reader.GetFieldValue<string>(2),
            reader.GetFieldValue<SubscriptionStatus>(3)
        );
    }
}
