using Beckett;
using Core.Commands;
using Core.Contracts;
using Core.Streams;
using Xunit;

namespace Core.Testing;

public class CommandHandlerSpecification<TCommand, TCommandHandler> where TCommand : ICommand
    where TCommandHandler : ICommandHandler<TCommand>, new()
{
    private readonly TCommandHandler _handler = new();
    private readonly List<object> _then = [];
    private Exception? _exception;

    public CommandHandlerSpecification<TCommand, TCommandHandler> When(TCommand command)
    {
        try
        {
            var events = _handler.Handle(command);

            _then.AddRange(events);
        }
        catch (Exception e)
        {
            _exception = e;
        }

        return this;
    }

    public void Then(params object[] events)
    {
        if (_exception != null)
        {
            throw _exception;
        }

        foreach (var expectedEvent in events.Select((value, index) => (Value: value, Index: index)))
        {
            var actualEvent = _then[new Index(expectedEvent.Index)];

            Assert.Equivalent(expectedEvent.Value, actualEvent, true);
        }
    }

    public void Then(params Action<object>[] assertions)
    {
        if (_exception != null)
        {
            throw _exception;
        }

        Assert.Collection(_then, assertions);
    }

    public void Throws<TException>(Action<TException>? assert = null) where TException : Exception
    {
        Assert.NotNull(_exception);

        var error = Assert.IsType<TException>(_exception);

        assert?.Invoke(error);
    }
}

public class CommandHandlerSpecification<TCommand, TCommandHandler, TState>
    where TCommand : ICommand
    where TCommandHandler : ICommandHandler<TCommand, TState>, new()
    where TState : class, IApply, new()
{
    private readonly TCommandHandler _handler = new();
    private readonly FakeStreamReader _reader = new();
    private readonly List<object> _given = [];
    private readonly List<object> _then = [];
    private Exception? _exception;

    public CommandHandlerSpecification<TCommand, TCommandHandler, TState> Given(params object[] events)
    {
        _given.AddRange(events);

        return this;
    }

    public CommandHandlerSpecification<TCommand, TCommandHandler, TState> Given(IStreamName streamName, params object[] events)
    {
        _reader.HasExistingStream(streamName, events);

        return this;
    }

    public CommandHandlerSpecification<TCommand, TCommandHandler, TState> When(TCommand command)
    {
        if (_given.Count > 0)
        {
            var streamName = _handler.StreamName(command);

            _reader.HasExistingStream(streamName, _given.ToArray());
        }

        var (state, _) = _handler.Load(command, _reader, CancellationToken.None).GetAwaiter().GetResult();

        try
        {
            var events = _handler.Handle(command, state);

            _then.AddRange(events);
        }
        catch (Exception e)
        {
            _exception = e;
        }

        return this;
    }

    public void Then(params object[] events)
    {
        if (_exception != null)
        {
            throw _exception;
        }

        foreach (var expectedEvent in events.Select((value, index) => (Value: value, Index: index)))
        {
            var actualEvent = _then[new Index(expectedEvent.Index)];

            Assert.Equivalent(expectedEvent.Value, actualEvent, true);
        }
    }

    public void Then(params Action<object>[] assertions)
    {
        if (_exception != null)
        {
            throw _exception;
        }

        Assert.Collection(_then, assertions);
    }

    public void Throws<TException>(Action<TException>? assert = null) where TException : Exception
    {
        Assert.NotNull(_exception);

        var error = Assert.IsType<TException>(_exception);

        assert?.Invoke(error);
    }
}
