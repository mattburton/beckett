using Beckett;

namespace Core.State;

public static class StateBuilder
{
    public static T Build<T>(params IMessageContext[] messages) where T : class, IApply, new() =>
        messages.Length == 0 ? new T() : messages.ProjectTo<T>();

    public static T Build<T>(params object[] messages) where T : class, IApply, new() =>
        messages.Length == 0 ? new T() : messages.Select(MessageContext.From).ProjectTo<T>();

    public static T Build<T>(T initialState, IEnumerable<object> messages) where T : class, IApply, new() =>
        messages.Select(MessageContext.From).ApplyTo(initialState);
}
