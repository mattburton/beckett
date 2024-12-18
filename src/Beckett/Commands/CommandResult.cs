namespace Beckett.Commands;

public readonly record struct CommandResult<TResult>(long StreamVersion, TResult Model);

public readonly record struct CommandResult(long StreamVersion);
