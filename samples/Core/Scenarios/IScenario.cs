using Beckett;
using Core.Commands;
using Core.Contracts;
using Core.State;

namespace Core.Scenarios;

public interface IScenario;

public class Scenario(string name) : IScenario
{
    public Given Given(params IEventType[] events)
    {
        return new Given(name, events);
    }
}

public class Given(string name, IEventType[] events)
{
    private object _actual = null!;
    private object _expected = null!;

    public When When(ICommand command)
    {
        return new When(name, events, command);
    }

    public When When<TState>(ICommand<TState> command) where TState : class, IApply, new()
    {
        return new When(name, events, command);
    }

    public End Then<T>(T expected) where T : class, IApply, new()
    {
        _actual = StateBuilder.Build(new T(), events);
        _expected = expected;

        return new End(name, events, _actual, _expected);
    }
}

public class When(string name, IEventType[] history, ICommandDispatcher command)
{
    public End Then(params IEventType[] events)
    {
        return new End(name, history, command, events, null);
    }

    public End Throws<TException>() where TException : Exception
    {
        var exceptionType = typeof(TException);

        return new End(name, history, command, null, exceptionType);
    }
}

public class End : IScenario
{
    public End(string name, IEventType[] history, ICommandDispatcher command, IEventType[]? events, Type? exceptionType)
    {
        _name = name;
        _history = history;
        _command = command;
        _events = events;
        _exceptionType = exceptionType;
    }

    public End(string name, IEventType[] history, object actual, object expected)
    {
        _name = name;
        _history = history;
        _actual = actual;
        _expected = expected;
    }

    private readonly string _name;
    private readonly IEventType[] _history;
    private readonly ICommandDispatcher? _command;
    private readonly IEventType[]? _events;
    private readonly Type? _exceptionType;
    private readonly object? _actual;
    private readonly object? _expected;

    public static implicit operator ScenarioParameters(End x) => new(
        x._name,
        x._history,
        x._command,
        x._events,
        x._exceptionType,
        x._actual,
        x._expected
    );
}

public record ScenarioParameters(
    string Name,
    IEventType[] History,
    ICommandDispatcher? Command,
    IEventType[]? Events,
    Type? ExceptionType,
    object? Actual,
    object? Expected
);
