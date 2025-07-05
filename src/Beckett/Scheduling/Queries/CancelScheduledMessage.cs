using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Scheduling.Queries;

public class CancelScheduledMessage(Guid id) : IPostgresDatabaseQuery<int>
{
    public async Task<int> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        const string sql = "DELETE FROM beckett.scheduled_messages WHERE id = $1;";

        command.CommandText = Query.Build(nameof(CancelScheduledMessage), sql, out var prepare);

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Uuid });

        if (prepare)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = id;

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
