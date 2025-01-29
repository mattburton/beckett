using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Subscriptions.Queries;

public class GetOrAddSubscription(
    int groupId,
    string name,
    PostgresOptions options
) : IPostgresDatabaseQuery<GetOrAddSubscription.Result>
{
    public async Task<Result> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        command.CommandText = $"select id, status from {options.Schema}.get_or_add_subscription($1, $2);";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });

        if (options.PrepareStatements)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = groupId;
        command.Parameters[1].Value = name;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        await reader.ReadAsync(cancellationToken);

        return new Result(reader.GetFieldValue<int>(0), reader.GetFieldValue<SubscriptionStatus>(1));
    }

    public record Result(int Id, SubscriptionStatus Status);
}
