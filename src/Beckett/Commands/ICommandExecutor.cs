namespace Beckett.Commands;

public interface ICommandExecutor
{
    /// <summary>
    /// Execute a command using the specified stream. The resulting events from the command will be appended to the
    /// stream using the current version of the stream as the expected version.
    /// </summary>
    /// <param name="streamName">The name of the stream to use when executing the command</param>
    /// <param name="command">The command to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<CommandResult> Execute(string streamName, ICommand command, CancellationToken cancellationToken);

    /// <summary>
    /// Execute the command using state built from the specified stream. The resulting events from the command will be
    /// appended to the stream using the current version of the stream as the expected version.
    /// </summary>
    /// <param name="streamName">The name of the stream to use when executing the command</param>
    /// <param name="command">The command to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <typeparam name="TState">The state of the stream that can be used to make decisions in the command</typeparam>
    Task<CommandResult> Execute<TState>(string streamName, ICommand<TState> command, CancellationToken cancellationToken)
        where TState : class, IApply, new();

    /// <summary>
    /// Execute a command using the specified stream. The resulting events from the command will be appended to the
    /// stream using the current version of the stream as the expected version. The updated stream will then be
    /// projected to the specified <typeparamref name="TResult"/> and returned as the result of the call. This is
    /// commonly used when you want to "read your writes", i.e. execute a command and return a response immediately to
    /// the caller.
    /// </summary>
    /// <param name="streamName">The name of the stream to use when executing the command</param>
    /// <param name="command">The command to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <typeparam name="TResult">The result model that will be built from the state of the stream after the command executes</typeparam>
    Task<CommandResult<TResult>> Execute<TResult>(
        string streamName,
        ICommand command,
        CancellationToken cancellationToken
    ) where TResult : IApply, new();

    /// <summary>
    /// Execute the command using state built from the specified stream. The resulting events from the command will be
    /// appended to the stream using the current version of the stream as the expected version. The updated stream will
    /// then be projected to the specified <typeparamref name="TResult"/> and returned as the result of the call. This
    /// is commonly used when you want to "read your writes", i.e. execute a command and return a response immediately
    /// to the caller.
    /// </summary>
    /// <param name="streamName">The name of the stream to use when executing the command</param>
    /// <param name="command">The command to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <typeparam name="TState">The state of the stream that can be used to make decisions in the command</typeparam>
    /// <typeparam name="TResult">The result model that will be built from the state of the stream after the command executes</typeparam>
    Task<CommandResult<TResult>> Execute<TState, TResult>(
        string streamName,
        ICommand<TState> command,
        CancellationToken cancellationToken
    ) where TState : class, IApply, new() where TResult : IApply, new();

    /// <summary>
    /// Execute a command using the specified stream. The resulting events from the command will be appended to the
    /// stream using the current version of the stream as the expected version unless overridden in the supplied
    /// options.
    /// </summary>
    /// <param name="streamName">The name of the stream to use when executing the command</param>
    /// <param name="command">The command to execute</param>
    /// <param name="options">Additional options to control the command execution process</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<CommandResult> Execute(string streamName, ICommand command, CommandOptions options, CancellationToken cancellationToken);

    /// <summary>
    /// Execute the command using state built from the specified stream. The resulting events from the command will be
    /// appended to the stream using the current version of the stream as the expected version unless overridden in the
    /// supplied options.
    /// </summary>
    /// <param name="streamName">The name of the stream to use when executing the command</param>
    /// <param name="command">The command to execute</param>
    /// <param name="options">Additional options to control the command execution process</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <typeparam name="TState">The state of the stream that can be used to make decisions in the command</typeparam>
    Task<CommandResult> Execute<TState>(
        string streamName,
        ICommand<TState> command,
        CommandOptions options,
        CancellationToken cancellationToken
    ) where TState : class, IApply, new();

    /// <summary>
    /// Execute a command using the specified stream. The resulting events from the command will be appended to the
    /// stream using the current version of the stream as the expected version unless overridden in the supplied
    /// options. The updated stream will then be projected to the specified <typeparamref name="TResult"/> and returned
    /// as the result of the call. This is commonly used when you want to "read your writes", i.e. execute a command and
    /// return a response immediately to the caller.
    /// </summary>
    /// <param name="streamName">The name of the stream to use when executing the command</param>
    /// <param name="command">The command to execute</param>
    /// <param name="options">Additional options to control the command execution process</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <typeparam name="TResult">The result model that will be built from the state of the stream after the command executes</typeparam>
    Task<CommandResult<TResult>> Execute<TResult>(
        string streamName,
        ICommand command,
        CommandOptions options,
        CancellationToken cancellationToken
    ) where TResult : IApply, new();

    /// <summary>
    /// Execute the command using state built from the specified stream. The resulting events from the command will be
    /// appended to the stream using the current version of the stream as the expected version unless overridden in the
    /// supplied options. The updated stream will then be projected to the specified <typeparamref name="TResult"/> and
    /// returned as the result of the call. This is commonly used when you want to "read your writes", i.e. execute a
    /// command and return a response immediately to the caller.
    /// </summary>
    /// <param name="streamName">The name of the stream to use when executing the command</param>
    /// <param name="command">The command to execute</param>
    /// <param name="options">Additional options to control the command execution process</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <typeparam name="TState">The state of the stream that can be used to make decisions in the command</typeparam>
    /// <typeparam name="TResult">The result model that will be built from the state of the stream after the command executes</typeparam>
    Task<CommandResult<TResult>> Execute<TState, TResult>(
        string streamName,
        ICommand<TState> command,
        CommandOptions options,
        CancellationToken cancellationToken
    ) where TState : class, IApply, new() where TResult : IApply, new();
}
