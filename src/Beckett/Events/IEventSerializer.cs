namespace Beckett.Events;

public interface IEventSerializer
{
    (Type Type, string TypeName, string Data, string Metadata) Serialize(
        object @event,
        Dictionary<string, object> metadata
    );
}
