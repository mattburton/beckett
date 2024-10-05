using System.Collections.Concurrent;

namespace Beckett;

public static class MessageTypeMap
{
    private static readonly ConcurrentDictionary<string, Type?> NameToTypeMap = new();
    private static readonly ConcurrentDictionary<Type, string> TypeToNameMap = new();

    public static void Map<TMessage>(string name)
    {
        var type = typeof(TMessage);

        Map(type, name);
    }

    public static void Map(Type type, string name)
    {
        if (NameToTypeMap.TryGetValue(name, out var existingType) && existingType != type)
        {
            throw new Exception($"Message type name {type.Name} for {type} already mapped to {existingType}");
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

        throw new UnknownTypeException(type.FullName!);
    }

    public static bool TryGetName(Type type, out string? name)
    {
        if (TypeToNameMap.TryGetValue(type, out var result))
        {
            name = result;

            return true;
        }

        name = null;

        return false;
    }

    public static bool TryGetType(string name, out Type? type)
    {
        if (NameToTypeMap.TryGetValue(name, out var mappedType))
        {
            type = mappedType;

            return true;
        }

        type = null;

        return false;
    }
}

public class UnknownTypeException(string typeName) : Exception($"Unknown type name: {typeName}");
