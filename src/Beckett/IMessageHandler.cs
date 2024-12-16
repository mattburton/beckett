namespace Beckett;

public interface IMessageHandler
{
    Task Handle(IMessageContext context, CancellationToken cancellationToken);
}

public interface IMessageHandler<in T> : IMessageHandlerAdapter
{
    Task Handle(T message, IMessageContext context, CancellationToken cancellationToken);

    Task IMessageHandlerAdapter.Handle(IMessageContext context, CancellationToken cancellationToken)
    {
        var instance = context.Message ??
                       throw new InvalidOperationException($"Unable to deserialize message of type: {context.Type}");

        if (instance is not T message)
        {
            throw new InvalidOperationException($"Expected message of type: {typeof(T)}, but got {instance.GetType()}");
        }

        return Handle(message, context, cancellationToken);
    }
}

public interface IMessageHandlerAdapter
{
    Task Handle(IMessageContext context, CancellationToken cancellationToken);
}
