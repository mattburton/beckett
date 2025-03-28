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
        Type messageHandlerType,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken
    )
    {
        Console.WriteLine($"MESSAGE: {context.Type}, lag: {DateTimeOffset.UtcNow.Subtract(context.Timestamp).TotalMilliseconds}ms");

        using var scope = serviceProvider.CreateScope();

        var handler = scope.ServiceProvider.GetRequiredService(messageHandlerType);
        var resultProcessor = scope.ServiceProvider.GetRequiredService<IResultProcessor>();

        if (handler is not IProcessorDispatcher dispatcher)
        {
            return;
        }

        var result = await dispatcher.Dispatch(context, cancellationToken);

        await resultProcessor.Process(result, cancellationToken);
    }
}
