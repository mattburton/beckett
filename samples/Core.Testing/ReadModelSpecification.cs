using Beckett;
using Xunit;

namespace Core.Testing;

public class ReadModelSpecification<T> where T : class, IApply, new()
{
    private readonly List<IMessageContext> _given = [];
    private T _then = new();

    public ReadModelSpecification<T> Given(params object[] events)
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
