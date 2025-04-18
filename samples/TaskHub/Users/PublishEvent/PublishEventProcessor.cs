namespace Users.PublishEvent;

public class PublishEventProcessor(IStreamReader reader) : IBatchProcessor
{
    public async Task<ProcessorResult> Handle(IReadOnlyList<IMessageContext> batch, CancellationToken cancellationToken)
    {
        var result = new ProcessorResult();

        foreach (var context in batch)
        {
            if (!context.StreamName.StartsWith(UserStream.Category))
            {
                continue;
            }

            var stream = await reader.ReadStream(context.StreamName, cancellationToken);

            var model = stream.ProjectTo<EventToPublishReadModel>();

            result.Publish(model.ToExternalEvent());
        }

        return result;
    }
}
