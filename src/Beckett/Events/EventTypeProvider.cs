using System.Reflection;

namespace Beckett.Events;

public class EventTypeProvider : IEventTypeProvider
{
    private readonly Lazy<Type[]> _types = new(LoadTypes);

    public Type? FindMatchFor(Predicate<Type> criteria)
    {
        return Array.Find(_types.Value, criteria);
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
