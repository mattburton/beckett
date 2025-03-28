using Beckett;
using Core.Contracts;

namespace Core.Processors;

public interface IProcessor<in TMessage> : IProcessorDispatcher where TMessage : class, ISupportSubscriptions
{
    Task<ProcessorResult> Handle(IMessageContext<TMessage> context, CancellationToken cancellationToken);

    async Task<ProcessorResult> IProcessorDispatcher.Dispatch(
        IMessageContext context,
        CancellationToken cancellationToken
    )
    {
        if (context.Message is not TMessage)
        {
            throw new InvalidOperationException($"Invalid message type: {context.Type}");
        }

        return await Handle(new MessageContext<TMessage>(context), cancellationToken) ??
               throw new InvalidOperationException("Processor returned null");
    }
}

public interface IProcessor : IProcessorDispatcher
{
    Task<ProcessorResult> Handle(IMessageContext context, CancellationToken cancellationToken);

    async Task<ProcessorResult> IProcessorDispatcher.Dispatch(
        IMessageContext context,
        CancellationToken cancellationToken
    )
    {
        return await Handle(context, cancellationToken) ??
               throw new InvalidOperationException("Processor returned null");
    }
}

public interface IProcessorDispatcher
{
    Task<ProcessorResult> Dispatch(IMessageContext context, CancellationToken cancellationToken);
}
