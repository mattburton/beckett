using Microsoft.Extensions.Logging;
using Npgsql;

namespace Beckett.Database;

public interface INotificationListener
{
    Task Listen(string channel, NotificationEventHandler eventHandler, CancellationToken cancellationToken);
}

public class NotificationListener(BeckettOptions options, ILogger<NotificationListener> logger) : INotificationListener
{
    public async Task Listen(string channel, NotificationEventHandler eventHandler, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await using var connection = new NpgsqlConnection(options.Database.ListenerConnectionString);

                connection.Notification += eventHandler;

                try
                {
                    await connection.OpenAsync(cancellationToken);

                    await using var command = new NpgsqlCommand($"LISTEN \"{channel}\";", connection);

                    await command.ExecuteNonQueryAsync(cancellationToken);

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
}
