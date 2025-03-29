using Beckett;
using Core.Contracts;
using Core.Streams;
using Microsoft.Extensions.DependencyInjection;

namespace Core.MessageHandling;

public class Dispatcher(
    IServiceProvider serviceProvider,
    IStreamReader reader,
    IMessageStore messageStore
) : IDispatcher
{
    private static readonly Type QueryHandlerRegistrationType = typeof(IQueryHandler<,>);

    public async Task Dispatch<TCommand>(
        TCommand command,
        CancellationToken cancellationToken
    ) where TCommand : ICommand
    {
        using var scope = serviceProvider.CreateScope();

        var dispatcher = scope.ServiceProvider.GetRequiredService<ICommandHandlerDispatcher<TCommand>>();

        var result = await dispatcher.Dispatch(command, reader, cancellationToken);

        if (result.Events.Count == 0)
        {
            return;
        }

        try
        {
            await messageStore.AppendToStream(
                result.StreamName.StreamName(),
                result.ExpectedVersion,
                result.Events,
                cancellationToken
            );
        }
        catch (StreamAlreadyExistsException)
        {
            throw new ResourceAlreadyExistsException();
        }
        catch (StreamDoesNotExistException)
        {
            throw new ResourceNotFoundException();
        }
    }

    public async Task<TResult> Dispatch<TResult>(IQuery<TResult> query, CancellationToken cancellationToken)
    {
        var handler = (IQueryHandler)serviceProvider.GetRequiredService(
            QueryHandlerRegistrationType.MakeGenericType(query.GetType(), typeof(TResult))
        );

        var result = await handler.Handle(query, cancellationToken);

        return (TResult)result!;
    }
}
