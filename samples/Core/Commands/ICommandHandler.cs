using Beckett;
using Core.Contracts;
using Core.Streams;

namespace Core.Commands;

public interface ICommandHandler<in TCommand, TState> : ICommandHandlerDispatcher<TCommand>
    where TCommand : ICommand where TState : class, IApply, new()
{
    IStreamName StreamName(TCommand command);

    async Task<(TState, ExpectedVersion)> Load(
        TCommand command,
        IStreamReader reader,
        CancellationToken cancellationToken
    )
    {
        var streamName = StreamName(command);

        var stream = await reader.ReadStream(streamName, cancellationToken);

        var state = stream.ProjectTo<TState>();

        return (state, ExpectedVersion.For(stream.Version));
    }

    IEnumerable<IEvent> Handle(TCommand command, TState state);

    async Task<CommandHandlerResult> ICommandHandlerDispatcher<TCommand>.Dispatch(
        TCommand command,
        IStreamReader reader,
        CancellationToken cancellationToken
    )
    {
        var streamName = StreamName(command);

        var (state, expectedVersion) = await Load(command, reader, cancellationToken);

        var events = Handle(command, state);

        return new CommandHandlerResult(streamName, expectedVersion, events.ToArray());
    }
}

public interface ICommandHandler<in TCommand> : ICommandHandlerDispatcher<TCommand>
    where TCommand : ICommand
{
    IStreamName StreamName(TCommand command);

    ExpectedVersion StreamVersion(TCommand command);

    IEnumerable<IEvent> Handle(TCommand command);

    Task<CommandHandlerResult> ICommandHandlerDispatcher<TCommand>.Dispatch(
        TCommand command,
        IStreamReader reader,
        CancellationToken cancellationToken
    )
    {
        var streamName = StreamName(command);

        var expectedVersion = StreamVersion(command);

        var events = Handle(command);

        return Task.FromResult(new CommandHandlerResult(streamName, expectedVersion, events.ToArray()));
    }
}

public interface ICommandHandlerDispatcher<in TCommand> : ICommandHandlerDispatcher where TCommand : ICommand
{
    Task<CommandHandlerResult> Dispatch(
        TCommand command,
        IStreamReader reader,
        CancellationToken cancellationToken
    );

    Task<CommandHandlerResult> ICommandHandlerDispatcher.Dispatch(
        object message,
        IStreamReader reader,
        CancellationToken cancellationToken
    )
    {
        if (message is not TCommand command)
        {
            throw new InvalidOperationException($"Invalid command type: {message.GetType()}");
        }

        return Dispatch(command, reader, cancellationToken);
    }
}

public interface ICommandHandlerDispatcher
{
    Task<CommandHandlerResult> Dispatch(
        object message,
        IStreamReader reader,
        CancellationToken cancellationToken
    );
}

public record CommandHandlerResult(
    IStreamName StreamName,
    ExpectedVersion ExpectedVersion,
    IReadOnlyList<IEvent> Events);
