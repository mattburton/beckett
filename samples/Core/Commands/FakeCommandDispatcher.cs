using Core.Contracts;

namespace Core.Commands;

public class FakeCommandDispatcher : ICommandDispatcher
{
    private Exception? _exception;

    public object? Received { get; private set; }

    public Task Dispatch<TCommand>(TCommand command, CancellationToken cancellationToken) where TCommand : ICommand
    {
        if (_exception is not null)
        {
            throw _exception;
        }

        Received = command;

        return Task.CompletedTask;
    }

    public void Throws<TException>(TException exception) where TException : Exception => _exception = exception;
}
