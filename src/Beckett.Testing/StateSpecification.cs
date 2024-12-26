using Xunit;

namespace Beckett.Testing;

public static class StateSpecification
{
    public static StateSpecification<T> For<T>() where T : IApply, new() => new();
}

public class StateSpecification<T> where T : IApply, new()
{
    private readonly List<object> _given = [];
    private T _then = new();

    public StateSpecification<T> Given(params object[] events)
    {
        _given.AddRange(events);

        return this;
    }

    public void Then(T expected)
    {
        _then = _given.ProjectTo<T>();

        Assert.Equivalent(expected, _then, true);
    }

    public void Then(Action<T> assert)
    {
        _then = _given.ProjectTo<T>();

        assert(_then);
    }
}
