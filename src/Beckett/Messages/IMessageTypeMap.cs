using Microsoft.Extensions.Logging;

namespace Beckett.Messages;

public interface IMessageTypeMap
{
    void Map<TMessage>(string name);
    void Map(Type type, string name);
    string GetName(Type type);
    Type? GetType(string name, ILoggerFactory loggerFactory);
    bool IsMapped(Type type);
}
