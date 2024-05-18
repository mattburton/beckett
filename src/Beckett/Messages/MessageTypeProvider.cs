using System.Reflection;

namespace Beckett.Messages;

public class MessageTypeProvider : IMessageTypeProvider
{
    private readonly Lazy<Type[]> _types = new(LoadTypes);

    public Type? FindMatchFor(Predicate<Type> criteria) => Array.Find(_types.Value, criteria);

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
