using System.Reflection;

namespace Beckett.Events;

public static class EventTypeProvider
{
    private static Type[] _types = [];

    public static void Initialize(IEnumerable<Assembly> assemblies)
    {
        _types = assemblies.SelectMany(x =>
        {
            try
            {
                return x.GetTypes();
            }
            catch (ReflectionTypeLoadException)
            {
                return Array.Empty<Type>();
            }
        }).ToArray();
    }

    public static Type? FindMatchFor(Predicate<Type> criteria)
    {
        return Array.Find(_types, criteria);
    }
}
