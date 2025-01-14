using Beckett.Messages;

namespace TaskHub.Infrastructure.Commands;

public class CommandExecutor(IMessageStore messageStore) : ICommandExecutor
{
    public async Task<CommandResult> Execute(string streamName, ICommand command, CancellationToken cancellationToken)
    {
        var stream = await ExecuteInternal(streamName, command, null, cancellationToken);

        return new CommandResult(stream.StreamVersion);
    }

    public async Task<CommandResult> Execute<TState>(
        string streamName,
        ICommand<TState> command,
        CancellationToken cancellationToken
    )
        where TState : class, IApply, new()
    {
        var stream = await ExecuteInternal(streamName, command, null, cancellationToken);

        return new CommandResult(stream.StreamVersion);
    }

    public async Task<CommandResult<TResult>> Execute<TResult>(
        string streamName,
        ICommand command,
        CancellationToken cancellationToken
    )
        where TResult : class, IApply, new()
    {
        var stream = await ExecuteInternal(streamName, command, null, cancellationToken);

        return new CommandResult<TResult>(stream.StreamVersion, stream.Messages.ProjectTo<TResult>());
    }

    public async Task<CommandResult<TResult>> Execute<TState, TResult>(
        string streamName,
        ICommand<TState> command,
        CancellationToken cancellationToken
    ) where TState : class, IApply, new() where TResult : class, IApply, new()
    {
        var stream = await ExecuteInternal(streamName, command, null, cancellationToken);

        return new CommandResult<TResult>(stream.StreamVersion, stream.Messages.ProjectTo<TResult>());
    }

    public async Task<CommandResult> Execute(
        string streamName,
        ICommand command,
        CommandOptions options,
        CancellationToken cancellationToken
    )
    {
        var stream = await ExecuteInternal(streamName, command, options, cancellationToken);

        return new CommandResult(stream.StreamVersion);
    }

    public async Task<CommandResult> Execute<TState>(
        string streamName,
        ICommand<TState> command,
        CommandOptions options,
        CancellationToken cancellationToken
    ) where TState : class, IApply, new()
    {
        var stream = await ExecuteInternal(streamName, command, options, cancellationToken);

        return new CommandResult(stream.StreamVersion);
    }

    public async Task<CommandResult<TResult>> Execute<TResult>(
        string streamName,
        ICommand command,
        CommandOptions options,
        CancellationToken cancellationToken
    ) where TResult : class, IApply, new()
    {
        var stream = await ExecuteInternal(streamName, command, options, cancellationToken);

        return new CommandResult<TResult>(stream.StreamVersion, stream.Messages.ProjectTo<TResult>());
    }

    public async Task<CommandResult<TResult>> Execute<TState, TResult>(
        string streamName,
        ICommand<TState> command,
        CommandOptions options,
        CancellationToken cancellationToken
    ) where TState : class, IApply, new() where TResult : class, IApply, new()
    {
        var stream = await ExecuteInternal(streamName, command, options, cancellationToken);

        return new CommandResult<TResult>(stream.StreamVersion, stream.Messages.ProjectTo<TResult>());
    }

    private async Task<ExecuteResult> ExecuteInternal(
        string streamName,
        ICommand command,
        CommandOptions? options,
        CancellationToken cancellationToken
    )
    {
        var stream = await ReadStream(streamName, options, cancellationToken);

        var result = command.Execute().ToList();

        if (result.Count == 0)
        {
            return new ExecuteResult(stream.StreamMessages, stream.StreamVersion);
        }

        return await AppendToStream(streamName, result, stream, options, cancellationToken);
    }

    private async Task<ExecuteResult> ExecuteInternal<TState>(
        string streamName,
        ICommand<TState> command,
        CommandOptions? options,
        CancellationToken cancellationToken
    ) where TState : class, IApply, new()
    {
        var stream = await ReadStream(streamName, options, cancellationToken);

        var state = stream.ProjectTo<TState>();

        var result = command.Execute(state).ToList();

        if (result.Count == 0)
        {
            return new ExecuteResult(stream.StreamMessages, stream.StreamVersion);
        }

        return await AppendToStream(streamName, result, stream, options, cancellationToken);
    }

    private async Task<IMessageStream> ReadStream(
        string streamName,
        CommandOptions? options,
        CancellationToken cancellationToken
    )
    {
        var stream = await messageStore.ReadStream(
            streamName,
            options?.ReadOptions ?? ReadOptions.Default,
            cancellationToken
        );

        if (stream.IsNotEmpty && options?.ExpectedVersion == ExpectedVersion.StreamDoesNotExist)
        {
            throw new StreamAlreadyExistsException("Stream already exists");
        }

        if (stream.IsEmpty && options?.ExpectedVersion == ExpectedVersion.StreamExists)
        {
            throw new StreamDoesNotExistException("Stream does not exist");
        }

        return stream;
    }

    private async Task<ExecuteResult> AppendToStream(
        string streamName,
        List<object> result,
        IMessageStream stream,
        CommandOptions? options,
        CancellationToken cancellationToken
    )
    {
        IAppendResult appendResult;

        if (options?.ExpectedVersion == ExpectedVersion.Any)
        {
            appendResult = await messageStore.AppendToStream(
                streamName,
                ExpectedVersion.Any,
                result,
                cancellationToken
            );
        }
        else
        {
            appendResult = await stream.Append(result, cancellationToken);
        }

        var messages = stream.StreamMessages.ToList();

        messages.AddRange(result.Select(MessageContext.From));

        return new ExecuteResult(messages, appendResult.StreamVersion);
    }

    private record ExecuteResult(IReadOnlyList<IMessageContext> Messages, long StreamVersion);
}
