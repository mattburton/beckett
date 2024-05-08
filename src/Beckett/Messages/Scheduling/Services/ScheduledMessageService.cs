using Beckett.Database;
using Beckett.Database.Queries;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Beckett.Messages.Scheduling.Services;

public class ScheduledMessageService(
    IPostgresDatabase database,
    ScheduledMessageOptions options,
    IPostgresMessageDeserializer messageDeserializer,
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
                    new GetScheduledMessagesToDeliver(options.BatchSize),
                    connection,
                    transaction,
                    stoppingToken
                );

                var scehduledMessages = new List<IScheduledMessageContext>();

                foreach (var streamGroup in results.GroupBy(x => x.StreamName))
                {
                    foreach (var scheduledMessage in streamGroup)
                    {
                        var (data, metadata) = messageDeserializer.DeserializeAll(scheduledMessage);

                        scehduledMessages.Add(new ScheduledMessageContext(scheduledMessage.StreamName, data, metadata));
                    }
                }

                foreach (var scheduledMessagesForStream in scehduledMessages.GroupBy(x => x.StreamName))
                {
                    var messages = scheduledMessagesForStream.Select(x => x.Message.WithMetadata(x.Metadata));

                    await messageStore.AppendToStream(
                        scheduledMessagesForStream.Key,
                        ExpectedVersion.Any,
                        messages,
                        stoppingToken
                    );
                }

                await transaction.CommitAsync(stoppingToken);

                await Task.Delay(options.PollingInterval, stoppingToken);
            }
            catch (OperationCanceledException e) when (e.CancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (TaskCanceledException e) when (e.CancellationToken.IsCancellationRequested)
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
