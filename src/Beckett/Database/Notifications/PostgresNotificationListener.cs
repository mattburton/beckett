using System.Data;
using System.Timers;
using Microsoft.Extensions.Logging;
using Npgsql;
using Timer = System.Timers.Timer;

namespace Beckett.Database.Notifications;

public class PostgresNotificationListener(
    NpgsqlDataSource dataSource,
    IEnumerable<IPostgresNotificationHandler> notificationHandlers,
    ILogger<PostgresNotificationListener> logger
) : IPostgresNotificationListener
{
    private readonly Dictionary<string, IPostgresNotificationHandler> _notificationHandlers =
        notificationHandlers.ToDictionary(x => x.Channel, x => x);

    private CancellationToken? _cancellationToken;

    public async Task Listen(CancellationToken stoppingToken)
    {
        if (_notificationHandlers.Count == 0)
        {
            return;
        }

        _cancellationToken = stoppingToken;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var connection = dataSource.CreateConnection();

                connection.Notification += NotificationEventHandler;

                using var keepAlive = new Timer(TimeSpan.FromSeconds(10).TotalMilliseconds);

                try
                {
                    await connection.OpenAsync(stoppingToken);

                    keepAlive.Elapsed += KeepAlive(connection);

                    var sql = string.Join(';', _notificationHandlers.Keys.Select(x => $"LISTEN \"{x}\""));

                    await using var command = new NpgsqlCommand(sql, connection);

                    await command.ExecuteNonQueryAsync(stoppingToken);

                    keepAlive.Enabled = true;

                    while (true)
                    {
                        try
                        {
                            await connection.WaitAsync(stoppingToken);
                        }
                        catch (NpgsqlException e)
                        {
                            logger.LogError(e, "Database error - will retry in 10 seconds");

                            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                        }
                    }
                }
                catch (NpgsqlException e)
                {
                    logger.LogError(e, "Database error - will retry in 10 seconds");

                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                }
                finally
                {
                    connection.Notification -= NotificationEventHandler;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error listening for notifications");
            }
        }
    }

    private void NotificationEventHandler(object sender, NpgsqlNotificationEventArgs e)
    {
        if (!_notificationHandlers.TryGetValue(e.Channel, out var handler))
        {
            return;
        }

        handler.Handle(e.Payload, _cancellationToken.GetValueOrDefault());
    }

    private static ElapsedEventHandler KeepAlive(NpgsqlConnection connection) =>
        (_, _) =>
        {
            if (connection.FullState == (ConnectionState.Open | ConnectionState.Fetching))
            {
                return;
            }

            using var command = connection.CreateCommand();

            command.CommandText = "select true;";

            command.ExecuteNonQuery();
        };
}
