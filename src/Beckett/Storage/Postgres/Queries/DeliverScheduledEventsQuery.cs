using Npgsql;
using NpgsqlTypes;

namespace Beckett.Storage.Postgres.Queries;

internal static class DeliverScheduledEventsQuery
{
    public static async Task Execute(
        NpgsqlConnection connection,
        string schema,
        bool sendPollingNotification,
        CancellationToken cancellationToken
    )
    {
        await using var command = connection.CreateCommand();

        command.CommandText = $"select {schema}.deliver_scheduled_events($1);";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Boolean });

        await command.PrepareAsync(cancellationToken);

        command.Parameters[0].Value = sendPollingNotification;

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
