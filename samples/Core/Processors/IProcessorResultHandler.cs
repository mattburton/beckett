namespace Core.Processors;

public interface IProcessorResultHandler
{
    Task Handle(ProcessorResult result, CancellationToken cancellationToken);
}
