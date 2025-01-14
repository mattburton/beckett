namespace TaskHub.Infrastructure.Queries;

public class QueryExecutor(IServiceProvider serviceProvider) : IQueryExecutor
{
    private static readonly Type QueryHandlerType = typeof(IQueryHandler<,>);

    public async Task<TResult?> Execute<TResult>(IQuery<TResult> query, CancellationToken cancellationToken)
        where TResult : class
    {
        var handler = (IQueryHandler)serviceProvider.GetRequiredService(
            QueryHandlerType.MakeGenericType(query.GetType(), typeof(TResult))
        );

        var result = await handler.Handle(query, cancellationToken);

        return result as TResult;
    }
}