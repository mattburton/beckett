namespace Beckett;

public interface IMessageBatch
{
    string StreamName { get; }

    IReadOnlyList<StreamMessage> StreamMessages { get; }

    IReadOnlyList<object> Messages { get; }
}

public class MessageBatch(string streamName, IReadOnlyList<StreamMessage> streamMessages) : IMessageBatch
{
    public string StreamName { get; } = streamName;

    public IReadOnlyList<StreamMessage> StreamMessages { get; } = streamMessages;

    public IReadOnlyList<object> Messages { get; } =
        streamMessages.Where(x => x.Message != null).Select(x => x.Message!).ToList();
}
