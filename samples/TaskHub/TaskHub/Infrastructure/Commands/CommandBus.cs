using Beckett.Messages;

namespace TaskHub.Infrastructure.Commands;

public class CommandBus(IMessageStore messageStore) : ICommandBus
{
    public async Task<CommandResult> Send(ICommand command, CancellationToken cancellationToken)
    {
        var stream = await ExecuteInternal(command.StreamName(), command, cancellationToken);

        return new CommandResult(stream.StreamVersion);
    }

    public async Task<CommandResult> Send<TState>(ICommand<TState> command, CancellationToken cancellationToken)
        where TState : class, IApply, new()
    {
        var stream = await ExecuteInternal(command.StreamName(), command, cancellationToken);

        return new CommandResult(stream.StreamVersion);
    }

    public async Task<CommandResult<TResult>> Send<TResult>(ICommand command, CancellationToken cancellationToken)
        where TResult : class, IApply, new()
    {
        var stream = await ExecuteInternal(command.StreamName(), command, cancellationToken);

        return new CommandResult<TResult>(stream.StreamVersion, stream.Messages.ProjectTo<TResult>());
    }

    public async Task<CommandResult<TResult>> Send<TState, TResult>(
        ICommand<TState> command,
        CancellationToken cancellationToken
    ) where TState : class, IApply, new() where TResult : class, IApply, new()
    {
        var stream = await ExecuteInternal(command.StreamName(), command, cancellationToken);

        return new CommandResult<TResult>(stream.StreamVersion, stream.Messages.ProjectTo<TResult>());
    }

    private async Task<ExecuteResult> ExecuteInternal(
        string streamName,
        ICommand command,
        CancellationToken cancellationToken
    )
    {
        var stream = await ReadStream(streamName, command.ExpectedVersion, command.ReadOptions, cancellationToken);

        var result = command.Execute().ToList();

        if (result.Count == 0)
        {
            return new ExecuteResult(stream.StreamMessages, stream.StreamVersion);
        }

        return await AppendToStream(streamName, stream, command.ExpectedVersion, result, cancellationToken);
    }

    private async Task<ExecuteResult> ExecuteInternal<TState>(
        string streamName,
        ICommand<TState> command,
        CancellationToken cancellationToken
    ) where TState : class, IApply, new()
    {
        var stream = await ReadStream(streamName, command.ExpectedVersion, command.ReadOptions, cancellationToken);

        var state = stream.ProjectTo<TState>();

        var result = command.Execute(state).ToList();

        if (result.Count == 0)
        {
            return new ExecuteResult(stream.StreamMessages, stream.StreamVersion);
        }

        return await AppendToStream(streamName, stream, command.ExpectedVersion, result, cancellationToken);
    }

    private async Task<IMessageStream> ReadStream(
        string streamName,
        ExpectedVersion expectedVersion,
        ReadOptions readOptions,
        CancellationToken cancellationToken
    )
    {
        var stream = await messageStore.ReadStream(
            streamName,
            readOptions,
            cancellationToken
        );

        if (stream.IsNotEmpty && expectedVersion == ExpectedVersion.StreamDoesNotExist)
        {
            throw new StreamAlreadyExistsException("Stream already exists");
        }

        if (stream.IsEmpty && expectedVersion == ExpectedVersion.StreamExists)
        {
            throw new StreamDoesNotExistException("Stream does not exist");
        }

        return stream;
    }

    private async Task<ExecuteResult> AppendToStream(
        string streamName,
        IMessageStream stream,
        ExpectedVersion expectedVersion,
        List<object> result,
        CancellationToken cancellationToken
    )
    {
        IAppendResult appendResult;

        if (expectedVersion == ExpectedVersion.Any)
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
