namespace Tests.Infrastructure.Tests;

public class FakeCommandBus : ICommandBus
{
    private Exception? _exception;
    private object? _result;

    public object? Received { get; private set; }

    public Task<CommandResult> Send(ICommand command, CancellationToken cancellationToken)
    {
        if (_exception is not null)
        {
            throw _exception;
        }

        Received = command;

        var result = (CommandResult)(_result ?? new CommandResult(1));

        return Task.FromResult(result);
    }

    public Task<CommandResult> Send<TState>(ICommand<TState> command, CancellationToken cancellationToken)
        where TState : class, IApply, new()
    {
        if (_exception is not null)
        {
            throw _exception;
        }

        Received = command;

        var result = (CommandResult)(_result ?? new CommandResult(1));

        return Task.FromResult(result);
    }

    public Task<CommandResult<TResult>> Send<TResult>(ICommand command, CancellationToken cancellationToken)
        where TResult : class, IApply, new()
    {
        if (_exception is not null)
        {
            throw _exception;
        }

        Received = command;

        var result = (CommandResult<TResult>)(_result ?? new CommandResult<TResult>(1, default!));

        return Task.FromResult(result);
    }

    public Task<CommandResult<TResult>> Send<TState, TResult>(
        ICommand<TState> command,
        CancellationToken cancellationToken
    ) where TState : class, IApply, new() where TResult : class, IApply, new()
    {
        if (_exception is not null)
        {
            throw _exception;
        }

        Received = command;

        var result = (CommandResult<TResult>)(_result ?? new CommandResult<TResult>(1, null!));

        return Task.FromResult(result);
    }

    public void Returns(CommandResult result) => _result = result;

    public void Returns<TResult>(CommandResult<TResult> result) => _result = result;

    public void Throws<TException>(TException exception) where TException : Exception => _exception = exception;
}
