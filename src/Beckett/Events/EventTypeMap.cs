using System.Collections.Concurrent;

namespace Beckett.Events;

public class EventTypeMap(EventOptions options, IEventTypeProvider eventTypeProvider) : IEventTypeMap
{
    private readonly ConcurrentDictionary<string, Type?> _nameToTypeMap = new();
    private readonly ConcurrentDictionary<Type, string> _typeToNameMap = new();

    public void Map<TEvent>(string name)
    {
        var type = typeof(TEvent);

        if (_nameToTypeMap.TryGetValue(name, out var existingType) && existingType != type)
        {
            throw new Exception($"Event type name {type.Name} for {type} already mapped to {existingType}");
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
                throw new Exception($"Event type name {type.Name} for {type} already mapped to {existingType}");
            }
        }

        _nameToTypeMap.TryAdd(type.Name, type);
        _typeToNameMap.TryAdd(type, type.Name);

        return _typeToNameMap[type];
    }

    public Type? GetType(string name)
    {
        return _nameToTypeMap.GetOrAdd(
            name,
            typeName =>
            {
                if (!options.AllowDynamicTypeMapping)
                {
                    throw new InvalidOperationException($"Missing event type mapping for {name}");
                }

                return eventTypeProvider.FindMatchFor(x => MatchCriteria(x, typeName));
            }
        );
    }

    private bool MatchCriteria(Type type, string name)
    {
        return type.Name == name || type.FullName == name;
    }
}
