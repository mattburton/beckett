namespace Beckett;

public interface IMessageBatchHandler
{
    Task Handle(IMessageBatch batch, CancellationToken cancellationToken);
}
