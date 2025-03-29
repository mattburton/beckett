namespace Core.MessageHandling;

public interface IProcessorResultHandler
{
    Task Process(ProcessorResult result, CancellationToken cancellationToken);
}
