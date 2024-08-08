using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Beckett.Messages;

public class MessageTypeMap(
    MessageOptions options,
    IMessageTypeProvider messageTypeProvider
) : IMessageTypeMap
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

        if (!options.AllowDynamicTypeMapping)
        {
            throw new Exception(
                $"{nameof(MessageOptions.AllowDynamicTypeMapping)} is disabled - you must add a type mapping for {type}"
            );
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

    public Type? GetType(string name, ILoggerFactory loggerFactory)
    {
        var messageType = _nameToTypeMap.GetOrAdd(
            name,
            typeName =>
            {
                if (!options.AllowDynamicTypeMapping)
                {
                    throw new Exception(
                        $"{nameof(MessageOptions.AllowDynamicTypeMapping)} is disabled - you must add a type mapping for {name}"
                    );
                }

                return messageTypeProvider.FindMatchFor(x => MatchCriteria(x, typeName));
            }
        );

        if (messageType != null)
        {
            return messageType;
        }

        switch (options.UnknownMessageTypePolicy)
        {
            case UnknownMessageTypePolicy.IgnoreAndContinue:
            {
                return null;
            }
            case UnknownMessageTypePolicy.LogErrorAndContinue:
                var loggerForContinue = loggerFactory.CreateLogger<MessageTypeMap>();

                loggerForContinue.LogError("Unknown message type: {MessageType} - continuing", name);

                return null;
            case UnknownMessageTypePolicy.LogErrorAndExitApplication:
                var loggerForExit = loggerFactory.CreateLogger<MessageTypeMap>();

                loggerForExit.LogError("Unknown message type: {MessageType} - exiting application", name);

                Environment.Exit(-1);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return null;
    }

    public bool IsMapped(Type type) => _typeToNameMap.ContainsKey(type);

    private static bool MatchCriteria(Type type, string name) => type.Name == name || type.FullName == name;
}
