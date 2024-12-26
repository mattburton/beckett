namespace Beckett;

public static class StateExtensions
{
    public static TState ProjectTo<TState>(this IEnumerable<object> messages)
        where TState : IApply, new() =>
        messages.Aggregate(
            new TState(),
            (current, message) =>
            {
                current.Apply(message);

                return current;
            }
        );

    public static TState ApplyTo<TState>(this IEnumerable<object> messages, TState state)
        where TState : IApply, new() =>
        messages.Aggregate(
            state,
            (current, message) =>
            {
                current.Apply(message);

                return current;
            }
        );
}
