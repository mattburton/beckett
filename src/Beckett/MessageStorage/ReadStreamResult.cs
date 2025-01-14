namespace Beckett.MessageStorage;

public record ReadStreamResult(string StreamName, long StreamVersion, IReadOnlyList<StreamMessage> StreamMessages);
