namespace Beckett.Commands;

public class CommandInvoker(IMessageStore messageStore) : ICommandInvoker
{
    public Task Execute(string streamName, ICommand command, CancellationToken cancellationToken)
    {
        return ExecuteInternal(streamName, command, null, cancellationToken);
    }

    public Task Execute<TState>(string streamName, ICommand<TState> command, CancellationToken cancellationToken)
        where TState : class, IApply, new()
    {
        return ExecuteInternal(streamName, command, null, cancellationToken);
    }

    public async Task<TResult> Execute<TResult>(
        string streamName,
        ICommand command,
        CancellationToken cancellationToken
    )
        where TResult : IApply, new()
    {
        var stream = await ExecuteInternal(streamName, command, null, cancellationToken);

        return stream.ProjectTo<TResult>();
    }

    public async Task<TResult> Execute<TState, TResult>(
        string streamName,
        ICommand<TState> command,
        CancellationToken cancellationToken
    ) where TState : class, IApply, new() where TResult : IApply, new()
    {
        var stream = await ExecuteInternal(streamName, command, null, cancellationToken);

        return stream.ProjectTo<TResult>();
    }

    public Task Execute(
        string streamName,
        ICommand command,
        CommandOptions options,
        CancellationToken cancellationToken
    )
    {
        return ExecuteInternal(streamName, command, options, cancellationToken);
    }

    public Task Execute<TState>(
        string streamName,
        ICommand<TState> command,
        CommandOptions<TState> options,
        CancellationToken cancellationToken
    ) where TState : class, IApply, new()
    {
        return ExecuteInternal(streamName, command, options, cancellationToken);
    }

    public async Task<TResult> Execute<TResult>(
        string streamName,
        ICommand command,
        CommandOptions options,
        CancellationToken cancellationToken
    ) where TResult : IApply, new()
    {
        var stream = await ExecuteInternal(streamName, command, options, cancellationToken);

        return stream.ProjectTo<TResult>();
    }

    public async Task<TResult> Execute<TState, TResult>(
        string streamName,
        ICommand<TState> command,
        CommandOptions<TState> options,
        CancellationToken cancellationToken
    ) where TState : class, IApply, new() where TResult : IApply, new()
    {
        var stream = await ExecuteInternal(streamName, command, options, cancellationToken);

        return stream.ProjectTo<TResult>();
    }

    private async Task<IReadOnlyList<object>> ExecuteInternal(
        string streamName,
        ICommand command,
        CommandOptions? options,
        CancellationToken cancellationToken
    )
    {
        var stream = await ReadStream(streamName, options, cancellationToken);

        var result = command.Execute().ToList();

        return await AppendToStream(streamName, result, stream, options, cancellationToken);
    }

    private async Task<IReadOnlyList<object>> ExecuteInternal<TState>(
        string streamName,
        ICommand<TState> command,
        CommandOptions<TState>? options,
        CancellationToken cancellationToken
    ) where TState : class, IApply, new()
    {
        var stream = await ReadStream(streamName, options, cancellationToken);

        var state = options?.State ?? stream.ProjectTo<TState>();

        var result = command.Execute(state).ToList();

        return await AppendToStream(streamName, result, stream, options, cancellationToken);
    }

    private async Task<MessageStream> ReadStream(
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

    private async Task<IReadOnlyList<object>> AppendToStream(
        string streamName,
        List<object> result,
        MessageStream stream,
        CommandOptions? options,
        CancellationToken cancellationToken
    )
    {
        if (options?.ExpectedVersion == ExpectedVersion.Any)
        {
            await messageStore.AppendToStream(streamName, ExpectedVersion.Any, result, cancellationToken);
        }
        else
        {
            await stream.Append(result, cancellationToken);
        }

        var messages = stream.Messages.ToList();

        messages.AddRange(result);

        return messages;
    }
}
