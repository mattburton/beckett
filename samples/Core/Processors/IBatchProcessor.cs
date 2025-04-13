using Beckett;

namespace Core.Processors;

public interface IBatchProcessor
{
    Task<ProcessorResult> Handle(IReadOnlyList<IMessageContext> batch, CancellationToken cancellationToken);
}
