using Core.Streams;

namespace TaskHub.Users.Processors.PublishNotification;

public class PublishNotificationProcessor(IStreamReader reader) : IProcessor
{
    public async Task<ProcessorResult> Handle(IMessageContext context, CancellationToken cancellationToken)
    {
        var stream = await reader.ReadStream(context.StreamName, cancellationToken);

        var model = stream.ProjectTo<NotificationToPublishReadModel>();

        var result = new ProcessorResult();

        result.Publish(model.ToNotification());

        return result;
    }
}
