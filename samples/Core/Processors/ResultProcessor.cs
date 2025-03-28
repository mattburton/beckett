using Beckett;
using Beckett.Messages;
using Beckett.MessageStorage.Postgres;
using Core.Batching;
using Core.Commands;
using Core.Contracts;
using Core.Streams;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Core.Processors;

public class ResultProcessor(
    NpgsqlDataSource dataSource,
    IBatchMessageStorage messageStorage,
    IBatchMessageScheduler messageScheduler,
    IServiceProvider serviceProvider
) : IResultProcessor
{
    private static readonly Type CommandHandlerDispatcherType = typeof(ICommandHandlerDispatcher<>);

    public async Task Process(ProcessorResult result, CancellationToken cancellationToken)
    {
        var batch = new NpgsqlBatch();

        if (result.Command != null)
        {
            await ExecuteCommand(result.Command, batch, cancellationToken);
        }

        PublishNotifications(result.Notifications, batch);

        ProcessJobs(result.Jobs, batch);

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        batch.Connection = connection;

        try
        {
            await batch.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (PostgresException e)
        {
            try
            {
                e.HandleAppendToStreamError();
            }
            catch (StreamAlreadyExistsException)
            {
                throw new ResourceAlreadyExistsException();
            }
            catch (StreamDoesNotExistException)
            {
                throw new ResourceNotFoundException();
            }

            throw;
        }
    }

    private void ProcessJobs(IReadOnlyList<IJob> jobs, NpgsqlBatch batch)
    {
        foreach (var job in jobs)
        {
            var actualJob = job switch
            {
                ScheduledJob s => s.Job,
                _ => job
            };

            var typeName = actualJob.TypeName();
            var partitionKey = actualJob.PartitionKey();
            var streamName = $"Jobs-{typeName}-{partitionKey}";
            var jobType = actualJob.GetType();
            var data = MessageSerializer.Serialize(jobType, actualJob);
            var enqueuedJob = new EnqueuedJob(typeName, data);

            if (job is ScheduledJob scheduledJob)
            {
                batch.BatchCommands.Add(
                    messageScheduler.ScheduleMessage(
                        streamName,
                        new Message(enqueuedJob),
                        DateTimeOffset.UtcNow.Add(scheduledJob.Delay)
                    )
                );
            }
            else
            {
                batch.BatchCommands.Add(
                    messageStorage.AppendToStream(
                        streamName,
                        ExpectedVersion.Any,
                        [new Message(enqueuedJob)]
                    )
                );
            }
        }
    }

    private void PublishNotifications(IReadOnlyList<INotification> notifications, NpgsqlBatch batch)
    {
        foreach (var notification in notifications)
        {
            var streamName = $"Notifications-{notification.TypeName()}-{notification.PartitionKey()}";

            batch.BatchCommands.Add(
                messageStorage.AppendToStream(
                    streamName,
                    ExpectedVersion.Any,
                    [new Message(notification)]
                )
            );
        }
    }

    private async Task ExecuteCommand(
        ICommand command,
        NpgsqlBatch batch,
        CancellationToken cancellationToken
    )
    {
        using var scope = serviceProvider.CreateScope();

        var commandType = command.GetType();

        var handlerRegistrationType = CommandHandlerDispatcherType.MakeGenericType(commandType);

        var handler = scope.ServiceProvider.GetRequiredService(handlerRegistrationType);

        var messageStreamReader = scope.ServiceProvider.GetRequiredService<IStreamReader>();

        if (handler is not ICommandHandlerDispatcher dispatcher)
        {
            return;
        }

        var result = await dispatcher.Dispatch(command, messageStreamReader, cancellationToken);

        ProcessCommandResult(result, batch);
    }

    private void ProcessCommandResult(CommandHandlerResult result, NpgsqlBatch batch)
    {
        if (result.Events.Count == 0)
        {
            return;
        }

        batch.BatchCommands.Add(
            messageStorage.AppendToStream(
                result.StreamName.StreamName(),
                result.ExpectedVersion,
                result.Events.Select(x => new Message(x)).ToArray()
            )
        );
    }
}
