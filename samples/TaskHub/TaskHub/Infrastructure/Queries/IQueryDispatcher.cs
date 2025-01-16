namespace TaskHub.Infrastructure.Queries;

public interface IQueryDispatcher
{
    Task<TResult?> Dispatch<TResult>(IQuery<TResult> query, CancellationToken cancellationToken) where TResult : class;
}
