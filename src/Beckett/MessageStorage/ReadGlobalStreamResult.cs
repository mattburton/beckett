namespace Beckett.MessageStorage;

public record ReadGlobalStreamResult(IReadOnlyList<StreamMessage> StreamMessages);
