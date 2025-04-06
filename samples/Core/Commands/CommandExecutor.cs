using Beckett;
using Core.Streams;

namespace Core.Commands;

public class CommandExecutor(IStreamReader reader, IMessageStore messageStore) : ICommandExecutor
{
    public async Task Execute<TCommand>(
        TCommand command,
        CancellationToken cancellationToken
    ) where TCommand : ICommand
    {
        ICommandDispatcher dispatcher = command;

        var result = await dispatcher.Dispatch(command, reader, cancellationToken);

        if (result.Events.Count == 0)
        {
            return;
        }

        await messageStore.AppendToStream(
            result.StreamName.StreamName(),
            result.ExpectedVersion,
            result.Events,
            cancellationToken
        );
    }

    public async Task Execute<TState>(ICommand<TState> command, CancellationToken cancellationToken)
        where TState : class, IApply, new()
    {
        ICommandDispatcher dispatcher = command;

        var result = await dispatcher.Dispatch(command, reader, cancellationToken);

        if (result.Events.Count == 0)
        {
            return;
        }

        await messageStore.AppendToStream(
            result.StreamName.StreamName(),
            result.ExpectedVersion,
            result.Events,
            cancellationToken
        );
    }
}
