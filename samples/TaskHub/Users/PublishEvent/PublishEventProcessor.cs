namespace Users.PublishEvent;

public class PublishEventProcessor(IStreamReader reader) : IProcessor
{
    public async Task<ProcessorResult> Handle(IMessageContext context, CancellationToken cancellationToken)
    {
        var stream = await reader.ReadStream(context.StreamName, cancellationToken);

        var model = stream.ProjectTo<EventToPublishReadModel>();

        var result = new ProcessorResult();

        result.Publish(model.ToExternalEvent());

        return result;
    }
}
