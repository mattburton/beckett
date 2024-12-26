using Beckett.Commands;

namespace Beckett.Testing;

public class FakeCommandExecutor : ICommandExecutor
{
    private object? _result;
    private Exception? _exception;

    public bool Executed { get; private set; }
    public string? ReceivedStreamName { get; private set; }
    public object? ReceivedCommand { get; private set; }
    public CommandOptions? ReceivedOptions { get; private set; }

    public void Returns(CommandResult result) => _result = result;

    public void Returns<TResult>(CommandResult<TResult> result) => _result = result;

    public void Throws<TException>(TException exception) where TException : Exception
    {
        _exception = exception;
    }

    public Task<CommandResult> Execute(string streamName, ICommand command, CancellationToken cancellationToken)
    {
        if (_exception is not null)
        {
            throw _exception;
        }

        Executed = true;
        ReceivedStreamName = streamName;
        ReceivedCommand = command;

        var result = (CommandResult)(_result ?? new CommandResult());

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

        Executed = true;
        ReceivedStreamName = streamName;
        ReceivedCommand = command;

        var result = (CommandResult)(_result ?? new CommandResult());

        return Task.FromResult(result);
    }

    public Task<CommandResult<TResult>> Execute<TResult>(
        string streamName,
        ICommand command,
        CancellationToken cancellationToken
    ) where TResult : IApply, new()
    {
        if (_exception is not null)
        {
            throw _exception;
        }

        Executed = true;
        ReceivedStreamName = streamName;
        ReceivedCommand = command;

        var result = (CommandResult<TResult>)(_result ?? new CommandResult<TResult>());

        return Task.FromResult(result);
    }

    public Task<CommandResult<TResult>> Execute<TState, TResult>(
        string streamName,
        ICommand<TState> command,
        CancellationToken cancellationToken
    ) where TState : class, IApply, new() where TResult : IApply, new()
    {
        if (_exception is not null)
        {
            throw _exception;
        }

        Executed = true;
        ReceivedStreamName = streamName;
        ReceivedCommand = command;

        var result = (CommandResult<TResult>)(_result ?? new CommandResult<TResult>());

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

        Executed = true;
        ReceivedStreamName = streamName;
        ReceivedCommand = command;
        ReceivedOptions = options;

        var result = (CommandResult)(_result ?? new CommandResult());

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

        Executed = true;
        ReceivedStreamName = streamName;
        ReceivedCommand = command;
        ReceivedOptions = options;

        var result = (CommandResult)(_result ?? new CommandResult());

        return Task.FromResult(result);
    }

    public Task<CommandResult<TResult>> Execute<TResult>(
        string streamName,
        ICommand command,
        CommandOptions options,
        CancellationToken cancellationToken
    ) where TResult : IApply, new()
    {
        if (_exception is not null)
        {
            throw _exception;
        }

        Executed = true;
        ReceivedStreamName = streamName;
        ReceivedCommand = command;
        ReceivedOptions = options;

        var result = (CommandResult<TResult>)(_result ?? new CommandResult<TResult>());

        return Task.FromResult(result);
    }

    public Task<CommandResult<TResult>> Execute<TState, TResult>(
        string streamName,
        ICommand<TState> command,
        CommandOptions options,
        CancellationToken cancellationToken
    ) where TState : class, IApply, new() where TResult : IApply, new()
    {
        if (_exception is not null)
        {
            throw _exception;
        }

        Executed = true;
        ReceivedStreamName = streamName;
        ReceivedCommand = command;
        ReceivedOptions = options;

        var result = (CommandResult<TResult>)(_result ?? new CommandResult<TResult>());

        return Task.FromResult(result);
    }
}
