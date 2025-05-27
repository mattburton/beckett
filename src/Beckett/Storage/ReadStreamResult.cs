namespace Beckett.Storage;

public record ReadStreamResult(string StreamName, long StreamVersion, IReadOnlyList<StreamMessage> StreamMessages);
