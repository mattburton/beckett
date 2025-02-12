using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Beckett.Messages;

public static class MessageTypeMap
{
    private static readonly ConcurrentDictionary<string, Type> NameToTypeMap = new();
    private static readonly ConcurrentDictionary<Type, string> TypeToNameMap = new();
    private static GetNameFallback? _getNameFallback;
    private static TryGetTypeFallback? _tryGetTypeFallback;

    static MessageTypeMap()
    {
        MapSystemTypes();
    }

    public static void Configure(GetNameFallback getNameFallback)
    {
        _getNameFallback = getNameFallback;
    }

    public static void Configure(TryGetTypeFallback tryGetTypeFallback)
    {
        _tryGetTypeFallback = tryGetTypeFallback;
    }

    public static void Map<TMessage>(string name)
    {
        var type = typeof(TMessage);

        Map(type, name);
    }

    public static void Map(Type type, string name)
    {
        if (NameToTypeMap.TryGetValue(name, out var existingType) && existingType != type)
        {
            throw new MessageTypeAlreadyMappedException(name, existingType.FullName!);
        }

        if (TypeToNameMap.TryGetValue(type, out var existingName) && existingName != name)
        {
            throw new MessageTypeAlreadyMappedException(name, existingName);
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

        name = _getNameFallback?.Invoke(type);

        if (name == null)
        {
            throw new UnknownTypeException(type.FullName!);
        }

        NameToTypeMap.TryAdd(name, type);
        TypeToNameMap.TryAdd(type, name);

        return name;
    }

    public static bool TryGetType(string name, [NotNullWhen(returnValue: true)] out Type? type)
    {
        if (NameToTypeMap.TryGetValue(name, out var mappedType))
        {
            type = mappedType;

            return true;
        }

        mappedType = _tryGetTypeFallback?.Invoke(name);

        if (mappedType == null)
        {
            type = null;

            return false;
        }

        type = mappedType;

        NameToTypeMap.TryAdd(name, type);
        TypeToNameMap.TryAdd(type, name);

        return true;
    }

    public static void Clear()
    {
        NameToTypeMap.Clear();
        TypeToNameMap.Clear();
    }

    private static void MapSystemTypes()
    {
        Map<StreamArchived>("$stream_archived");
        Map<StreamTruncated>("$stream_truncated");
    }
}

public delegate string GetNameFallback(Type type);

public delegate Type? TryGetTypeFallback(string name);

public class UnknownTypeException(string name) : Exception($"Unknown type name: {name}");

public class MessageTypeAlreadyMappedException(string name, string existingName)
    : Exception($"Message type name '{name}' is already mapped to {existingName}.");
