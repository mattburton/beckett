namespace Beckett.Storage;

public record ReadGlobalStreamResult(IReadOnlyList<GlobalStreamMessage> Messages);
