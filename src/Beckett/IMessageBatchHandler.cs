namespace Beckett;

public interface IMessageBatchHandler
{
    Task Handle(IReadOnlyList<IMessageContext> batch, CancellationToken cancellationToken);
}
