namespace TaskHub.Infrastructure.Commands;

public record CommandResult<TResult>(long StreamVersion, TResult Result);

public record CommandResult(long StreamVersion);
