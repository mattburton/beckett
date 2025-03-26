using Beckett;
using Beckett.Messages;
using Core.Commands;
using Xunit;

namespace Core.Testing;

public class CommandSpecification<TCommand> where TCommand : ICommand
{
    private readonly List<object> _then = [];
    private Exception? _exception;

    public CommandSpecification<TCommand> When(TCommand command)
    {
        try
        {
            var events = command.Execute();

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

public class CommandSpecification<TCommand, TState>
    where TCommand : ICommand<TState> where TState : class, IApply, new()
{
    private readonly List<IMessageContext> _given = [];
    private readonly List<object> _then = [];
    private Exception? _exception;

    public CommandSpecification<TCommand, TState> Given(params object[] events)
    {
        _given.AddRange(events.Select(MessageContext.From));

        return this;
    }

    public CommandSpecification<TCommand, TState> When(TCommand command)
    {
        var state = _given.ProjectTo<TState>();

        try
        {
            var events = command.Execute(state);

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
