namespace TaskLists.Tests;

public class FakeTaskListModule : ITaskListModule
{
    private Exception? _exception;
    private readonly Dictionary<object, object?> _expectedResults = [];

    public object? Received { get; private set; }

    public Task Execute<TCommand>(TCommand command, CancellationToken cancellationToken) where TCommand : ICommand
    {
        if (_exception is not null)
        {
            throw _exception;
        }

        Received = command;

        return Task.CompletedTask;
    }

    public Task Execute<TState>(ICommand<TState> command, CancellationToken cancellationToken)
        where TState : class, IApply, new()
    {
        if (_exception is not null)
        {
            throw _exception;
        }

        Received = command;

        return Task.CompletedTask;
    }

    public Task<TResult?> Execute<TResult>(IQuery<TResult> query, CancellationToken cancellationToken)
    {
        return (_expectedResults.TryGetValue(query, out var result)
            ? Task.FromResult((TResult?)result!)
            : Task.FromResult<TResult>(default!))!;
    }

    public void Returns<TResult>(IQuery<TResult> query, TResult? result) => _expectedResults[query] = result;

    public void Throws<TException>(TException exception) where TException : Exception => _exception = exception;
}
