namespace Beckett;

public static class StateExtensions
{
    public static TState ProjectTo<TState>(this IEnumerable<object> messages) where TState : IState, new()
    {
        return messages.Aggregate(new TState(), (current, message) =>
        {
            current.Apply(message);

            return current;
        });
    }

    public static TState ProjectTo<TState>(this TState seed, IEnumerable<object> messages) where TState : IState, new()
    {
        return messages.Aggregate(seed, (current, message) =>
        {
            current.Apply(message);

            return current;
        });
    }
}
