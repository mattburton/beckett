namespace Beckett.Commands;

public readonly record struct CommandResult<TResult>(long StreamVersion, TResult Result);

public readonly record struct CommandResult(long StreamVersion);
