using Npgsql;
using NpgsqlTypes;

namespace Beckett.Database.Queries;

public class CancelScheduledMessage(string application, Guid id) : IPostgresDatabaseQuery<int>
{
    public async Task<int> Execute(NpgsqlCommand command, string schema, CancellationToken cancellationToken)
    {
        command.CommandText = $"select {schema}.cancel_scheduled_message($1, $2);";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Uuid });

        await command.PrepareAsync(cancellationToken);

        command.Parameters[0].Value = application;
        command.Parameters[1].Value = id;

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
