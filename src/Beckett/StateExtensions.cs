namespace Beckett;

public static class StateExtensions
{
    public static TState ProjectTo<TState>(this IEnumerable<object> events) where TState : IState, new()
    {
        return events.Aggregate(new TState(), (current, @event) =>
        {
            current.Apply(@event);

            return current;
        });
    }

    public static TState ProjectTo<TState>(this TState seed, IEnumerable<object> events) where TState : IState, new()
    {
        return events.Aggregate(seed, (current, @event) =>
        {
            current.Apply(@event);

            return current;
        });
    }
}
