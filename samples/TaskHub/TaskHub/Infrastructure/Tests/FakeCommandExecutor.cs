namespace TaskHub.Infrastructure.Tests;

public class FakeCommandExecutor : ICommandExecutor
{
    private Exception? _exception;
    private object? _result;

    public ExecutedCommand? Received { get; private set; }

    public Task<CommandResult> Execute(string streamName, ICommand command, CancellationToken cancellationToken)
    {
        if (_exception is not null)
        {
            throw _exception;
        }

        Received = new ExecutedCommand(streamName, command, null);

        var result = (CommandResult)(_result ?? new CommandResult(1));

        return Task.FromResult(result);
    }

    public Task<CommandResult> Execute<TState>(
        string streamName,
        ICommand<TState> command,
        CancellationToken cancellationToken
    ) where TState : class, IApply, new()
    {
        if (_exception is not null)
        {
            throw _exception;
        }

        Received = new ExecutedCommand(streamName, command, null);

        var result = (CommandResult)(_result ?? new CommandResult(1));

        return Task.FromResult(result);
    }

    public Task<CommandResult<TResult>> Execute<TResult>(
        string streamName,
        ICommand command,
        CancellationToken cancellationToken
    ) where TResult : class, IApply, new()
    {
        if (_exception is not null)
        {
            throw _exception;
        }

        Received = new ExecutedCommand(streamName, command, null);

        var result = (CommandResult<TResult>)(_result ?? new CommandResult<TResult>(1, default!));

        return Task.FromResult(result);
    }

    public Task<CommandResult<TResult>> Execute<TState, TResult>(
        string streamName,
        ICommand<TState> command,
        CancellationToken cancellationToken
    ) where TState : class, IApply, new() where TResult : class, IApply, new()
    {
        if (_exception is not null)
        {
            throw _exception;
        }

        Received = new ExecutedCommand(streamName, command, null);

        var result = (CommandResult<TResult>)(_result ?? new CommandResult<TResult>(1, null!));

        return Task.FromResult(result);
    }

    public Task<CommandResult> Execute(
        string streamName,
        ICommand command,
        CommandOptions options,
        CancellationToken cancellationToken
    )
    {
        if (_exception is not null)
        {
            throw _exception;
        }

        Received = new ExecutedCommand(streamName, command, options);

        var result = (CommandResult)(_result ?? new CommandResult(1));

        return Task.FromResult(result);
    }

    public Task<CommandResult> Execute<TState>(
        string streamName,
        ICommand<TState> command,
        CommandOptions options,
        CancellationToken cancellationToken
    ) where TState : class, IApply, new()
    {
        if (_exception is not null)
        {
            throw _exception;
        }

        Received = new ExecutedCommand(streamName, command, options);

        var result = (CommandResult)(_result ?? new CommandResult(1));

        return Task.FromResult(result);
    }

    public Task<CommandResult<TResult>> Execute<TResult>(
        string streamName,
        ICommand command,
        CommandOptions options,
        CancellationToken cancellationToken
    ) where TResult : class, IApply, new()
    {
        if (_exception is not null)
        {
            throw _exception;
        }

        Received = new ExecutedCommand(streamName, command, options);

        var result = (CommandResult<TResult>)(_result ?? new CommandResult<TResult>(1, null!));

        return Task.FromResult(result);
    }

    public Task<CommandResult<TResult>> Execute<TState, TResult>(
        string streamName,
        ICommand<TState> command,
        CommandOptions options,
        CancellationToken cancellationToken
    ) where TState : class, IApply, new() where TResult : class, IApply, new()
    {
        if (_exception is not null)
        {
            throw _exception;
        }

        Received = new ExecutedCommand(streamName, command, options);

        var result = (CommandResult<TResult>)(_result ?? new CommandResult<TResult>(1, null!));

        return Task.FromResult(result);
    }

    public void Returns(CommandResult result) => _result = result;

    public void Returns<TResult>(CommandResult<TResult> result) => _result = result;

    public void Throws<TException>(TException exception) where TException : Exception => _exception = exception;

    public record ExecutedCommand(string StreamName, object Command, CommandOptions? Options);
}
