using Microsoft.Extensions.Logging;
using Npgsql;

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

    public async Task Listen(CancellationToken cancellationToken)
    {
        if (_notificationHandlers.Count == 0)
        {
            return;
        }

        _cancellationToken = cancellationToken;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await using var connection = dataSource.CreateConnection();

                connection.Notification += NotificationEventHandler;

                try
                {
                    await connection.OpenAsync(cancellationToken);

                    var sql = string.Join(';', _notificationHandlers.Keys.Select(x => $"LISTEN \"{x}\""));

                    await using var command = new NpgsqlCommand(sql, connection);

                    await command.ExecuteNonQueryAsync(cancellationToken);

                    while (true)
                    {
                        await connection.WaitAsync(cancellationToken);
                    }
                }
                finally
                {
                    connection.Notification -= NotificationEventHandler;
                }
            }
            catch (OperationCanceledException e) when (e.CancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error listening for notifications - will retry in 10 seconds");

                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
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
}
