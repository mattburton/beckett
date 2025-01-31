namespace TaskHub.Infrastructure.Queries;

public interface IQueryBus
{
    Task<TResult?> Send<TResult>(IQuery<TResult> query, CancellationToken cancellationToken) where TResult : class;
}
