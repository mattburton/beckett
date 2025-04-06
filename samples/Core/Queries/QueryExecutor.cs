using Microsoft.Extensions.DependencyInjection;

namespace Core.Queries;

public class QueryExecutor(IServiceProvider serviceProvider) : IQueryExecutor
{
    private static readonly Type QueryHandlerRegistrationType = typeof(IQueryHandler<,>);

    public async Task<TResult> Execute<TResult>(IQuery<TResult> query, CancellationToken cancellationToken)
    {
        var handler = (IQueryHandler)serviceProvider.GetRequiredService(
            QueryHandlerRegistrationType.MakeGenericType(query.GetType(), typeof(TResult))
        );

        var result = await handler.Handle(query, cancellationToken);

        return (TResult)result!;
    }
}
