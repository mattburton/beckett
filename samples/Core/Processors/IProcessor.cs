using Beckett;
using Core.Contracts;

namespace Core.Processors;

public interface IProcessor<in TMessage> : IProcessor where TMessage : class, IProcessorInput
{
    Task<ProcessorResult> Handle(IMessageContext<TMessage> context, CancellationToken cancellationToken);

    async Task<ProcessorResult> IProcessor.Handle(
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

public interface IProcessor
{
    Task<ProcessorResult> Handle(IMessageContext context, CancellationToken cancellationToken);
}
