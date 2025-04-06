using Beckett;

namespace Core.Commands;

public interface ICommandExecutor
{
    Task Execute<TCommand>(TCommand command, CancellationToken cancellationToken) where TCommand : ICommand;

    Task Execute<TState>(ICommand<TState> command, CancellationToken cancellationToken)
        where TState : class, IApply, new();
}
