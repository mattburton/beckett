namespace Beckett.Messages;

public interface IMessageTypeMap
{
    void Map<TMessage>(string name);
    string GetName(Type type);
    Type? GetType(string name);
    bool IsMapped(Type type);
}
