namespace Beckett.Storage;

public record ReadGlobalStreamResult(IReadOnlyList<StreamMessage> StreamMessages);
