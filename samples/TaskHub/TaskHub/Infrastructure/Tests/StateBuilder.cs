namespace TaskHub.Infrastructure.Tests;

public static class StateBuilder
{
    public static T Build<T>(params object[] messages) where T : class, IApply, new() =>
        messages.Length == 0 ? new T() : messages.ProjectTo<T>();
}
