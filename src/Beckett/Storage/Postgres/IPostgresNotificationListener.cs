using System.Timers;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Beckett.Storage.Postgres;

internal interface IPostgresNotificationListener
{
    Task Listen(string channel, NotificationEventHandler eventHandler, CancellationToken cancellationToken);
}

internal class PostgresNotificationListener(
    BeckettOptions options,
    IPostgresDatabase database,
    ILogger<PostgresNotificationListener> logger
) : IPostgresNotificationListener
{
    public async Task Listen(string channel, NotificationEventHandler eventHandler, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await using var connection = database.CreateConnection();

                connection.Notification += eventHandler;

                using var keepAlive = new System.Timers.Timer(options.Postgres.ListenerKeepAlive.TotalMilliseconds);

                try
                {
                    await connection.OpenAsync(cancellationToken);

                    keepAlive.Elapsed += KeepAlive(connection);

                    await using var command = new NpgsqlCommand($"LISTEN \"{channel}\";", connection);

                    await command.ExecuteNonQueryAsync(cancellationToken);

                    keepAlive.Enabled = true;

                    while (true)
                    {
                        await connection.WaitAsync(cancellationToken);
                    }
                }
                finally
                {
                    connection.Notification -= eventHandler;
                }
            }
            catch (OperationCanceledException e) when (e.CancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error listening for notifications");
            }
        }
    }

    private static ElapsedEventHandler KeepAlive(NpgsqlConnection connection)
    {
        return (_, _) =>
        {
            using var command = connection.CreateCommand();

            command.CommandText = "select true;";

            command.ExecuteNonQuery();
        };
    }
}
