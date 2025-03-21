using Beckett.Messages;

namespace TaskHub.Infrastructure.Tests;

public class StateSpecification<T> where T : class, IApply, new()
{
    private readonly List<IMessageContext> _given = [];
    private T _then = new();

    public StateSpecification<T> Given(params object[] events)
    {
        _given.AddRange(events.Select(MessageContext.From));

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
