namespace Beckett;

public static class StreamName
{
    public static string For<T>(object id) => $"{typeof(T).Name}.{id}";
}
