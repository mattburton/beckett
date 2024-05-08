namespace Beckett.Messages;

public interface IMessageSerializer
{
    (Type Type, string TypeName, string Data, string Metadata) Serialize(
        object message,
        Dictionary<string, object> metadata
    );
}
