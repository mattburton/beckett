using System.Reflection;

namespace Beckett.Subscriptions.Retries;

public static class ExceptionTypeProvider
{
    private static readonly Lazy<Type[]> Types = new(LoadTypes);

    public static Type? FindMatchFor(Predicate<Type> criteria) => Array.Find(Types.Value, criteria);

    private static Type[] LoadTypes() =>
        AppDomain.CurrentDomain.GetAssemblies().SelectMany(
            x =>
            {
                try
                {
                    return x.GetTypes();
                }
                catch (ReflectionTypeLoadException)
                {
                    return [];
                }
            }
        ).ToArray();
}
