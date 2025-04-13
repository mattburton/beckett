using Beckett;
using Beckett.Messages;
using Beckett.MessageStorage.Postgres;
using Core.Commands;
using Core.Contracts;
using Core.Jobs;
using Core.Streams;
using Npgsql;

namespace Core.Processors;

public class ProcessorResultHandler(
    NpgsqlDataSource dataSource,
    IBatchMessageStorage messageStorage,
    IBatchMessageScheduler messageScheduler,
    IStreamReader reader
) : IProcessorResultHandler
{
    public async Task Handle(ProcessorResult result, CancellationToken cancellationToken)
    {
        var batch = new NpgsqlBatch();

        if (result.Command != null)
        {
            await ExecuteCommand(result.Command, batch, cancellationToken);
        }

        PublishExternalEvents(result.ExternalEvents, batch);

        ProcessJobs(result.Jobs, batch);

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        batch.Connection = connection;

        try
        {
            await batch.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (PostgresException e)
        {
            e.HandleAppendToStreamError();

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

    private void PublishExternalEvents(IReadOnlyList<IExternalEvent> externalEvents, NpgsqlBatch batch)
    {
        foreach (var externalEvent in externalEvents)
        {
            var streamName = $"ExternalEvents-{externalEvent.TypeName()}-{externalEvent.PartitionKey()}";

            batch.BatchCommands.Add(
                messageStorage.AppendToStream(
                    streamName,
                    ExpectedVersion.Any,
                    [new Message(externalEvent)]
                )
            );
        }
    }

    private async Task ExecuteCommand(
        ICommandDispatcher command,
        NpgsqlBatch batch,
        CancellationToken cancellationToken
    )
    {
        var result = await command.Dispatch(command, reader, cancellationToken);

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
