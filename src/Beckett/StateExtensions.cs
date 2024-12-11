namespace Beckett;

public static class StateExtensions
{
    public static TState ProjectTo<TState>(this IEnumerable<object> messages, TState? state = default)
        where TState : IApply, new() =>
        messages.Aggregate(
            state ?? new TState(),
            (current, message) =>
            {
                current.Apply(message);

                return current;
            }
        );
}
