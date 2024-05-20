using Beckett.Database;
using Beckett.Database.Models;
using Beckett.Database.Queries;
using Cronos;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Beckett.Scheduling.Services;

public class RecurringMessageService(
    IPostgresDatabase database,
    BeckettOptions options,
    IPostgresMessageDeserializer messageDeserializer,
    IMessageStore messageStore,
    ILogger<RecurringMessageService> logger
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
                    new GetRecurringMessagesToDeliver(options.ApplicationName, options.Scheduling.BatchSize),
                    connection,
                    transaction,
                    stoppingToken
                );

                if (results.Count == 0)
                {
                    continue;
                }

                var recurringMessages = new List<ScheduledMessageContext>();

                foreach (var streamGroup in results.GroupBy(x => x.StreamName))
                foreach (var recurringMessage in streamGroup)
                {
                    var (data, metadata) = messageDeserializer.DeserializeAll(recurringMessage);

                    recurringMessages.Add(
                        new ScheduledMessageContext(
                            recurringMessage.StreamName,
                            data,
                            metadata
                        )
                    );
                }

                await UpdateRecurringMessageNextOccurrences(connection, transaction, results, stoppingToken);

                foreach (var recurringMessagesForStream in recurringMessages.GroupBy(x => x.StreamName))
                {
                    var messages = recurringMessagesForStream.Select(x => x.Message.WithMetadata(x.Metadata));

                    await messageStore.AppendToStream(
                        recurringMessagesForStream.Key,
                        ExpectedVersion.Any,
                        messages,
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

    private async Task UpdateRecurringMessageNextOccurrences(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        IReadOnlyList<PostgresRecurringMessage> results,
        CancellationToken stoppingToken
    )
    {
        await using var batch = new NpgsqlBatch(connection, transaction);

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(options.Scheduling.TimeZone);

        foreach (var recurringMessage in results)
        {
            UpdateRecurringMessageNextOccurrence(batch, recurringMessage, timeZone);
        }

        await batch.ExecuteNonQueryAsync(stoppingToken);
    }

    private void UpdateRecurringMessageNextOccurrence(
        NpgsqlBatch batch,
        PostgresRecurringMessage recurringMessage,
        TimeZoneInfo timeZone
    )
    {
        var command = batch.CreateBatchCommand();

        command.CommandText =
            $"select {options.Postgres.Schema}.update_recurring_message_next_occurrence($1, $2, $3, $4);";

        DateTimeOffset? nextOccurrence = null;

        if (recurringMessage.CronExpression != null)
        {
            var cronExpression = CronExpression.Parse(recurringMessage.CronExpression);

            nextOccurrence = cronExpression.GetNextOccurrence(DateTimeOffset.UtcNow, timeZone);
        }

        command.Parameters.Add(new NpgsqlParameter<string> { Value = recurringMessage.Application });
        command.Parameters.Add(new NpgsqlParameter<string> { Value = recurringMessage.Name });
        command.Parameters.Add(new NpgsqlParameter<DateTimeOffset?> { Value = nextOccurrence });
        command.Parameters.Add(new NpgsqlParameter<bool> { Value = nextOccurrence == null });

        batch.BatchCommands.Add(command);
    }
}
