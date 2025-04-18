namespace Beckett;

public static class StateExtensions
{
    public static TState ProjectTo<TState>(this IEnumerable<IMessageContext> messages)
        where TState : class, IApply, new() =>
        messages.Aggregate(
            new TState(),
            (current, message) =>
            {
                current.Apply(message);

                return current;
            }
        );

    public static TState ApplyTo<TState>(this IEnumerable<IMessageContext> messages, TState state)
        where TState : class, IApply, new() =>
        messages.Aggregate(
            state,
            (current, message) =>
            {
                current.Apply(message);

                return current;
            }
        );
}
