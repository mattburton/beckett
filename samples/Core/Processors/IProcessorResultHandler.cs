namespace Core.Processors;

public interface IProcessorResultHandler
{
    Task Process(ProcessorResult result, CancellationToken cancellationToken);
}
