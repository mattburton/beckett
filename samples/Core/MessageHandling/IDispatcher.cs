using Core.Contracts;

namespace Core.MessageHandling;

public interface IDispatcher
{
    Task Dispatch<TCommand>(TCommand command, CancellationToken cancellationToken) where TCommand : ICommand;
    Task<TResult> Dispatch<TResult>(IQuery<TResult> query, CancellationToken cancellationToken);
}
