using Beckett;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Processors;

public static class ProcessorHandler
{
    public static Func<IMessageContext, IServiceProvider, CancellationToken, Task> For(Type processorType)
    {
        return (context, serviceProvider, token) => Handle(context, processorType, serviceProvider, token);
    }

    private static async Task Handle(
        IMessageContext context,
        Type processorType,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken
    )
    {
        using var scope = serviceProvider.CreateScope();

        var instance = scope.ServiceProvider.GetRequiredService(processorType);
        var resultHandler = scope.ServiceProvider.GetRequiredService<IProcessorResultHandler>();

        if (instance is not IProcessor processor)
        {
            return;
        }

        var result = await processor.Handle(context, cancellationToken);

        await resultHandler.Handle(result, cancellationToken);
    }
}
