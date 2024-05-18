using System.Collections.Concurrent;

namespace Beckett.Messages;

public class MessageTypeMap(MessageOptions options, IMessageTypeProvider messageTypeProvider) : IMessageTypeMap
{
    private readonly ConcurrentDictionary<string, Type?> _nameToTypeMap = new();
    private readonly ConcurrentDictionary<Type, string> _typeToNameMap = new();

    public void Map<TMessage>(string name)
    {
        var type = typeof(TMessage);

        if (_nameToTypeMap.TryGetValue(name, out var existingType) && existingType != type)
        {
            throw new Exception($"Message type name {type.Name} for {type} already mapped to {existingType}");
        }

        _nameToTypeMap.TryAdd(name, type);
        _typeToNameMap.TryAdd(type, name);
    }

    public string GetName(Type type)
    {
        if (_typeToNameMap.TryGetValue(type, out var name))
        {
            return name;
        }

        //TODO - support custom type names, mapping from old names to new ones, etc...
        if (_nameToTypeMap.TryGetValue(type.Name, out var existingType))
        {
            if (existingType != type)
            {
                throw new Exception($"Message type name {type.Name} for {type} already mapped to {existingType}");
            }
        }

        _nameToTypeMap.TryAdd(type.Name, type);
        _typeToNameMap.TryAdd(type, type.Name);

        return _typeToNameMap[type];
    }

    public Type? GetType(string name) =>
        _nameToTypeMap.GetOrAdd(
            name,
            typeName =>
            {
                if (!options.AllowDynamicTypeMapping)
                {
                    throw new Exception($"Missing message type mapping for {name}");
                }

                return messageTypeProvider.FindMatchFor(x => MatchCriteria(x, typeName));
            }
        );

    private static bool MatchCriteria(Type type, string name) => type.Name == name || type.FullName == name;
}
