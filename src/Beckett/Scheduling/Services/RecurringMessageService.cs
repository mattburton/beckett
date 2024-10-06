using Beckett.Database;
using Beckett.Database.Models;
using Beckett.Scheduling.Queries;
using Cronos;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Beckett.Scheduling.Services;

public class RecurringMessageService(
    IPostgresDatabase database,
    BeckettOptions options,
    IMessageStore messageStore,
    ILogger<RecurringMessageService> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!RecurringMessageRegistry.Any())
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var connection = database.CreateConnection();

                await connection.OpenAsync(stoppingToken);

                await using var transaction = await connection.BeginTransactionAsync(stoppingToken);

                var results = await database.Execute(
                    new GetRecurringMessagesToDeliver(options.Scheduling.RecurringBatchSize),
                    connection,
                    transaction,
                    stoppingToken
                );

                if (results.Count == 0)
                {
                    await transaction.RollbackAsync(stoppingToken);

                    await Task.Delay(options.Scheduling.RecurringPollingInterval, stoppingToken);

                    continue;
                }

                var recurringMessages = new List<ScheduledMessageContext>();

                foreach (var streamGroup in results.GroupBy(x => x.StreamName))
                {
                    recurringMessages.AddRange(
                        streamGroup.Select(
                            recurringMessage => new ScheduledMessageContext(
                                recurringMessage.StreamName,
                                recurringMessage.ToMessage()
                            )
                        )
                    );
                }

                await UpdateRecurringMessageNextOccurrences(connection, transaction, results, stoppingToken);

                foreach (var recurringMessagesForStream in recurringMessages.GroupBy(x => x.StreamName))
                {
                    await messageStore.AppendToStream(
                        recurringMessagesForStream.Key,
                        ExpectedVersion.Any,
                        recurringMessagesForStream.Select(x => x.Message),
                        stoppingToken
                    );
                }

                await transaction.CommitAsync(stoppingToken);

                await Task.Delay(options.Scheduling.RecurringPollingInterval, stoppingToken);
            }
            catch (OperationCanceledException e) when (e.CancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Unhandled error delivering recurring messages - will try again in 10 seconds");

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }

    private async Task UpdateRecurringMessageNextOccurrences(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        IReadOnlyList<PostgresRecurringMessage> results,
        CancellationToken stoppingToken
    )
    {
        await using var batch = new NpgsqlBatch(connection, transaction);

        foreach (var recurringMessage in results)
        {
            UpdateRecurringMessageNextOccurrence(batch, recurringMessage);
        }

        await batch.ExecuteNonQueryAsync(stoppingToken);
    }

    private void UpdateRecurringMessageNextOccurrence(
        NpgsqlBatch batch,
        PostgresRecurringMessage recurringMessage
    )
    {
        var command = batch.CreateBatchCommand();

        command.CommandText =
            $"select {options.Postgres.Schema}.update_recurring_message_next_occurrence($1, $2);";

        var cronExpression = CronExpression.Parse(recurringMessage.CronExpression);

        var nextOccurrence = cronExpression.GetNextOccurrence(DateTimeOffset.UtcNow, TimeZoneInfo.Utc);

        command.Parameters.Add(new NpgsqlParameter<string> { Value = recurringMessage.Name });
        command.Parameters.Add(new NpgsqlParameter<DateTimeOffset> { Value = nextOccurrence, IsNullable = true });

        batch.BatchCommands.Add(command);
    }
}
