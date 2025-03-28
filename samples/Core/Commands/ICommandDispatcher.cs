using Core.Contracts;

namespace Core.Commands;

public interface ICommandDispatcher
{
    Task Dispatch<TCommand>(TCommand command, CancellationToken cancellationToken) where TCommand : ICommand;
}
