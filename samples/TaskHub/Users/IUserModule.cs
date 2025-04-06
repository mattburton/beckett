namespace Users;

public interface ICommand : Core.Commands.ICommand;

public interface ICommand<in TState> : Core.Commands.ICommand<TState> where TState : class, IApply, new();

public interface IQuery<TResult> : Core.Queries.IQuery<TResult>;

public interface IUserModule : IModule
{
    Task Execute<TCommand>(TCommand command, CancellationToken cancellationToken) where TCommand : ICommand;

    Task Execute<TState>(ICommand<TState> command, CancellationToken cancellationToken)
        where TState : class, IApply, new();

    Task<TResult?> Execute<TResult>(IQuery<TResult> query, CancellationToken cancellationToken);
}

public class UserModule(ICommandExecutor commandExecutor, IQueryExecutor queryExecutor) : IUserModule
{
    public async Task Execute<TCommand>(TCommand command, CancellationToken cancellationToken) where TCommand : ICommand
    {
        await commandExecutor.Execute(command, cancellationToken);
    }

    public async Task Execute<TState>(ICommand<TState> command, CancellationToken cancellationToken)
        where TState : class, IApply, new()
    {
        await commandExecutor.Execute(command, cancellationToken);
    }

    public async Task<TResult?> Execute<TResult>(IQuery<TResult> query, CancellationToken cancellationToken)
    {
        return await queryExecutor.Execute(query, cancellationToken);
    }
}
