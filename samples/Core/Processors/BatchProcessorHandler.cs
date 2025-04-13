using Beckett;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Processors;

public static class BatchProcessorHandler
{
    public static Func<IReadOnlyList<IMessageContext>, IServiceProvider, CancellationToken, Task> For(Type processorType)
    {
        return (batch, serviceProvider, token) => Handle(batch, processorType, serviceProvider, token);
    }

    private static async Task Handle(
        IReadOnlyList<IMessageContext> batch,
        Type processorType,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken
    )
    {
        using var scope = serviceProvider.CreateScope();

        var instance = scope.ServiceProvider.GetRequiredService(processorType);
        var resultHandler = scope.ServiceProvider.GetRequiredService<IProcessorResultHandler>();

        if (instance is not IBatchProcessor batchProcessor)
        {
            return;
        }

        var result = await batchProcessor.Handle(batch, cancellationToken);

        await resultHandler.Handle(result, cancellationToken);
    }
}
