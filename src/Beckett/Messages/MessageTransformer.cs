using System.Text.Json;
using System.Text.Json.Nodes;

namespace Beckett.Messages;

public static class MessageTransformer
{
    private static readonly Dictionary<MappedType, Func<JsonObject, JsonObject>> Transformations = [];
    private static readonly Dictionary<string, string> TypeMap = [];

    public static void Register(string oldTypeName, string newTypeName, Func<JsonObject, JsonObject> transformation)
    {
        if (!TypeMap.TryAdd(oldTypeName, newTypeName))
        {
            throw new TransformationAlreadyRegisteredException(oldTypeName, newTypeName);
        }

        Transformations.Add(new MappedType(oldTypeName, newTypeName), transformation);
    }

    public static JsonDocument Transform(string typeName, JsonDocument data)
    {
        var mappedTypes = FindAllMappedTypes(typeName).ToArray();

        if (mappedTypes.Length == 0)
        {
            return data;
        }

        var result = JsonObject.Create(data.RootElement) ??
                     throw new InvalidOperationException("Unable to create JsonObject from message data");

        foreach (var mappedType in mappedTypes)
        {
            if (!Transformations.TryGetValue(mappedType, out var transformation))
            {
                break;
            }

            result = transformation(result);
        }

        return JsonDocument.Parse(result.ToJsonString());
    }

    public static void Clear()
    {
        Transformations.Clear();
        TypeMap.Clear();
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

public class TransformationAlreadyRegisteredException(string oldTypeName, string newTypeName)
    : Exception($"Transformation from type '{oldTypeName}' to '{newTypeName} is already registered.");
