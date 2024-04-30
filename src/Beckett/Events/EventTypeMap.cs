using System.Collections.Concurrent;

namespace Beckett.Events;

public static class EventTypeMap
{
    private static readonly ConcurrentDictionary<string, Type?> NameToTypeMap = new();
    private static readonly ConcurrentDictionary<Type, string> TypeToNameMap = new();

    public static string GetName(Type type)
    {
        if (TypeToNameMap.TryGetValue(type, out var name))
        {
            return name;
        }

        //TODO - support custom type names, mapping from old names to new ones, etc...
        if (NameToTypeMap.TryGetValue(type.Name, out var existingType))
        {
            if (existingType != type)
            {
                throw new Exception($"Event type name {type.Name} for {type} already mapped to {existingType}");
            }
        }

        NameToTypeMap.TryAdd(type.Name, type);
        TypeToNameMap.TryAdd(type, type.Name);

        return TypeToNameMap[type];
    }

    public static Type? GetType(string name) => NameToTypeMap.GetOrAdd(
        name,
        typeName => { return EventTypeProvider.FindMatchFor(x => MatchCriteria(x, typeName)); }
    );

    private static bool MatchCriteria(Type type, string name)
    {
        return type.Name == name || type.FullName == name;
    }
}
