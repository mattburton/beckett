using Beckett.Database;
using Beckett.Scheduling.Queries;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Beckett.Scheduling.Services;

public class ScheduledMessageService(
    IPostgresDatabase database,
    BeckettOptions options,
    IMessageStore messageStore,
    ILogger<ScheduledMessageService> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var connection = database.CreateConnection();

                await connection.OpenAsync(stoppingToken);

                await using var transaction = await connection.BeginTransactionAsync(stoppingToken);

                var results = await database.Execute(
                    new GetScheduledMessagesToDeliver(options.Scheduling.BatchSize, options.Postgres),
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

                await Task.Delay(options.Scheduling.PollingInterval, stoppingToken);
            }
            catch (OperationCanceledException e) when (e.CancellationToken.IsCancellationRequested)
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
