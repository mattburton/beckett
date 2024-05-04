using System.Reflection;

namespace Beckett.Events;

public static class EventTypeProvider
{
    private static readonly Lazy<Type[]> Types = new(LoadTypes);

    public static Type? FindMatchFor(Predicate<Type> criteria)
    {
        return Array.Find(Types.Value, criteria);
    }

    private static Type[] LoadTypes()
    {
        return AppDomain.CurrentDomain.GetAssemblies().SelectMany(x =>
        {
            try
            {
                return x.GetTypes();
            }
            catch (ReflectionTypeLoadException)
            {
                return [];
            }
        }).ToArray();
    }
}
