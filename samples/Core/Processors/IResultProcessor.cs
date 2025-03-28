namespace Core.Processors;

public interface IResultProcessor
{
    Task Process(ProcessorResult result, CancellationToken cancellationToken);
}
