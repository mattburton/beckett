using Beckett.Database;
using Beckett.Scheduling.Queries;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Beckett.Scheduling.Services;

public class ScheduledMessageService(
    IPostgresDataSource dataSource,
    IPostgresDatabase database,
    BeckettOptions options,
    IMessageStore messageStore,
    ILogger<ScheduledMessageService> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var timer = new PeriodicTimer(options.Scheduling.PollingInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                stoppingToken.ThrowIfCancellationRequested();

                await using var connection = dataSource.CreateConnection();

                await connection.OpenAsync(stoppingToken);

                await using var transaction = await connection.BeginTransactionAsync(stoppingToken);

                var results = await database.Execute(
                    new GetScheduledMessagesToDeliver(options.Scheduling.BatchSize),
                    connection,
                    transaction,
                    stoppingToken
                );

                var scheduledMessages = new List<ScheduledMessageContext>();

                foreach (var streamGroup in results.GroupBy(x => x.StreamName))
                    scheduledMessages.AddRange(
                        streamGroup.Select(
                            scheduledMessage => new ScheduledMessageContext(
                                scheduledMessage.StreamName,
                                scheduledMessage.ToMessage()
                            )
                        )
                    );

                foreach (var scheduledMessagesForStream in scheduledMessages.GroupBy(x => x.StreamName))
                {
                    await messageStore.AppendToStream(
                        scheduledMessagesForStream.Key,
                        ExpectedVersion.Any,
                        scheduledMessagesForStream.Select(x => x.Message),
                        stoppingToken
                    );
                }

                await transaction.CommitAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Unhandled error delivering scheduled messages - will try again in 10 seconds");

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }
}
