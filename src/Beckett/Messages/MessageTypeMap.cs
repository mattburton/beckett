using System.Collections.Concurrent;

namespace Beckett.Messages;

public static class MessageTypeMap
{
    private static readonly ConcurrentDictionary<string, Type> NameToTypeMap = new();
    private static readonly ConcurrentDictionary<Type, string> TypeToNameMap = new();
    private static readonly ConcurrentDictionary<string, string> TranslationMap = [];
    private static GetNameFallback? _getNameFallback;
    private static TryGetTypeFallback? _tryGetTypeFallback;

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

        NameToTypeMap.TryAdd(name, type);
        TypeToNameMap.TryAdd(type, name);
    }

    public static void Map(string oldName, string newName)
    {
        if (oldName == newName)
        {
            throw new MessageTypesCannotBeMappedToThemselvesException(oldName);
        }

        if (TranslationMap.TryAdd(oldName, newName))
        {
            return;
        }

        var existingName = TranslationMap[oldName];

        throw new MessageTypeAlreadyMappedException(oldName, existingName);
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

    public static bool TryGetType(string name, out Type? type)
    {
        name = TranslateName(name);

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
        TranslationMap.Clear();
    }

    private static string TranslateName(string name)
    {
        while (true)
        {
            if (!TranslationMap.TryGetValue(name, out var result))
            {
                return name;
            }

            name = result;
        }
    }
}

public delegate string GetNameFallback(Type type);

public delegate Type? TryGetTypeFallback(string name);

public class UnknownTypeException(string name) : Exception($"Unknown type name: {name}");

public class MessageTypesCannotBeMappedToThemselvesException(string name)
    : Exception($"Message types cannot be mapped to themselves: {name}.");

public class MessageTypeAlreadyMappedException(string name, string existingName)
    : Exception($"Message type name '{name}' is already mapped to {existingName}.");
