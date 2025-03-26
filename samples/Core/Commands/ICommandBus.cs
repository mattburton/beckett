using Beckett;

namespace Core.Commands;

public interface ICommandBus
{
    /// <summary>
    /// Execute a command using the specified stream. The resulting events from the command will be appended to the
    /// stream using the current version of the stream as the expected version.
    /// </summary>
    /// <param name="command">The command to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<CommandResult> Send(ICommand command, CancellationToken cancellationToken);

    /// <summary>
    /// Execute the command using state built from the specified stream. The resulting events from the command will be
    /// appended to the stream using the current version of the stream as the expected version.
    /// </summary>
    /// <param name="command">The command to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <typeparam name="TState">The state of the stream that can be used to make decisions in the command</typeparam>
    Task<CommandResult> Send<TState>(ICommand<TState> command, CancellationToken cancellationToken)
        where TState : class, IApply, new();

    /// <summary>
    /// Execute a command using the specified stream. The resulting events from the command will be appended to the
    /// stream using the current version of the stream as the expected version. The updated stream will then be
    /// projected to the specified <typeparamref name="TResult" /> and returned as the result of the call. This is
    /// commonly used when you want to "read your writes", i.e. execute a command and return a response immediately to
    /// the caller.
    /// </summary>
    /// <param name="command">The command to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <typeparam name="TResult">The result model that will be built from the state of the stream after the command executes</typeparam>
    Task<CommandResult<TResult>> Send<TResult>(ICommand command, CancellationToken cancellationToken)
        where TResult : class, IApply, new();

    /// <summary>
    /// Execute the command using state built from the specified stream. The resulting events from the command will be
    /// appended to the stream using the current version of the stream as the expected version. The updated stream will
    /// then be projected to the specified <typeparamref name="TResult" /> and returned as the result of the call. This
    /// is commonly used when you want to "read your writes", i.e. execute a command and return a response immediately
    /// to the caller.
    /// </summary>
    /// <param name="command">The command to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <typeparam name="TState">The state of the stream that can be used to make decisions in the command</typeparam>
    /// <typeparam name="TResult">The result model that will be built from the state of the stream after the command executes</typeparam>
    Task<CommandResult<TResult>> Send<TState, TResult>(ICommand<TState> command, CancellationToken cancellationToken)
        where TState : class, IApply, new() where TResult : class, IApply, new();
}
