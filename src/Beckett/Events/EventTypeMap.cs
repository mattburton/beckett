using System.Collections.Concurrent;

namespace Beckett.Events;

public static class EventTypeMap
{
    private static readonly ConcurrentDictionary<string, Type?> NameToTypeMap = new();
    private static readonly ConcurrentDictionary<Type, string> TypeToNameMap = new();

    public static void Map<TEvent>(string name)
    {
        var type = typeof(TEvent);

        if (NameToTypeMap.TryGetValue(name, out var existingType) && existingType != type)
        {
            throw new Exception($"Event type name {type.Name} for {type} already mapped to {existingType}");
        }

        NameToTypeMap.TryAdd(name, type);
        TypeToNameMap.TryAdd(type, name);
    }

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

    public static Type? GetType(string name)
    {
        return NameToTypeMap.GetOrAdd(
            name,
            typeName => { return EventTypeProvider.FindMatchFor(x => MatchCriteria(x, typeName)); }
        );
    }

    private static bool MatchCriteria(Type type, string name)
    {
        return type.Name == name || type.FullName == name;
    }
}
