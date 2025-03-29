using Beckett;
using Core.Contracts;
using Core.State;

namespace Core.MessageHandling;

public interface IHaveScenarios
{
    IScenario[] Scenarios { get; }
}

public interface IScenario;

public class Scenario(string name) : IScenario
{
    public Given Given(params IEvent[] events)
    {
        return new Given(name, events);
    }
}

public class Given(string name, IEvent[] events)
{
    private object _actual = null!;
    private object _expected = null!;

    public When When(ICommand command)
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

public class End : IScenario
{
    public End(string name, IEvent[] history, ICommand command, IEvent[]? events, Type? exceptionType)
    {
        _name = name;
        _history = history;
        _command = command;
        _events = events;
        _exceptionType = exceptionType;
    }

    public End(string name, IEvent[] history, object actual, object expected)
    {
        _name = name;
        _history = history;
        _actual = actual;
        _expected = expected;
    }

    private readonly string _name;
    private readonly IEvent[] _history;
    private readonly ICommand? _command;
    private readonly IEvent[]? _events;
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

public class When(string name, IEvent[] history, ICommand command)
{
    public End Then(params IEvent[] events)
    {
        return new End(name, history, command, events, null);
    }

    public End Throws<TException>() where TException : Exception
    {
        var exceptionType = typeof(TException);

        return new End(name, history, command, null, exceptionType);
    }
}

public record ScenarioParameters(
    string Name,
    IEvent[] History,
    ICommand? Command,
    IEvent[]? Events,
    Type? ExceptionType,
    object? Actual,
    object? Expected
);

public static class Example
{
    public static readonly Guid Guid = Guid.NewGuid();
    public static readonly string String = Guid.NewGuid().ToString();
}
