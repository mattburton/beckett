namespace Beckett;

public static class ApplyExtensions
{
    public static T ProjectTo<T>(this IEnumerable<IMessageContext> messages)
        where T : class, IApply, new() =>
        messages.Aggregate(
            new T(),
            (current, message) =>
            {
                current.Apply(message);

                return current;
            }
        );

    public static T ApplyTo<T>(this IEnumerable<IMessageContext> messages, T instance)
        where T : class, IApply, new() =>
        messages.Aggregate(
            instance,
            (current, message) =>
            {
                current.Apply(message);

                return current;
            }
        );
}
