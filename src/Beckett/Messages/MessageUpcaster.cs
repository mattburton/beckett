using System.Text.Json;
using System.Text.Json.Nodes;

namespace Beckett.Messages;

public static class MessageUpcaster
{
    private static readonly Dictionary<MappedType, Func<JsonObject, JsonObject>> Upcasters = [];
    private static readonly Dictionary<string, string> TypeMap = [];

    public static void Register(string oldTypeName, string newTypeName, Func<JsonObject, JsonObject> transformation)
    {
        if (!TypeMap.TryAdd(oldTypeName, newTypeName))
        {
            throw new UpcasterAlreadyRegisteredException(oldTypeName, newTypeName);
        }

        Upcasters.Add(new MappedType(oldTypeName, newTypeName), transformation);
    }

    public static (string TypeName, JsonDocument Data) Upcast(string typeName, JsonDocument data)
    {
        var mappedTypes = FindAllMappedTypes(typeName).ToArray();

        if (mappedTypes.Length == 0)
        {
            return (typeName, data);
        }

        var result = JsonObject.Create(data.RootElement) ??
                     throw new InvalidOperationException("Unable to create JsonObject from message data");

        var newTypeName = typeName;

        foreach (var mappedType in mappedTypes)
        {
            if (!Upcasters.TryGetValue(mappedType, out var upcaster))
            {
                break;
            }

            newTypeName = mappedType.NewTypeName;
            result = upcaster(result);
        }

        return (newTypeName, JsonDocument.Parse(result.ToJsonString()));
    }

    public static void Clear()
    {
        Upcasters.Clear();
        TypeMap.Clear();
    }

    private static IEnumerable<MappedType> FindAllMappedTypes(string oldTypeName)
    {
        while (true)
        {
            if (!TypeMap.TryGetValue(oldTypeName, out var newTypeName))
            {
                yield break;
            }

            yield return new MappedType(oldTypeName, newTypeName);

            if (oldTypeName == newTypeName)
            {
                yield break;
            }

            oldTypeName = newTypeName;
        }
    }

    // ReSharper disable NotAccessedPositionalProperty.Local
    private readonly record struct MappedType(string OldTypeName, string NewTypeName);
}

public class UpcasterAlreadyRegisteredException(string oldTypeName, string newTypeName)
    : Exception($"Upcaster from type '{oldTypeName}' to '{newTypeName} is already registered.");
