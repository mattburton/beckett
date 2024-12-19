namespace Beckett;

public interface IMessageBatch
{
    IReadOnlyList<IMessageContext> StreamMessages { get; }
    IReadOnlyList<object> Messages { get; }
}

public interface IMessageBatchHandler
{
    Task Handle(IMessageBatch batch, CancellationToken cancellationToken);
}
