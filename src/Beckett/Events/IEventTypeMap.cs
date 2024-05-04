namespace Beckett.Events;

public interface IEventTypeMap
{
    void Map<TEvent>(string name);
    string GetName(Type type);
    Type? GetType(string name);
}
