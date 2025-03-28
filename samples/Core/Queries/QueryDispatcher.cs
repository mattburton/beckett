using Core.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Queries;

public class QueryDispatcher(IServiceProvider serviceProvider) : IQueryDispatcher
{
    private static readonly Type QueryHandlerType = typeof(IQueryHandler<,>);

    public async Task<TResult> Dispatch<TResult>(IQuery<TResult> query, CancellationToken cancellationToken)
    {
        var handler = (IQueryHandler)serviceProvider.GetRequiredService(
            QueryHandlerType.MakeGenericType(query.GetType(), typeof(TResult))
        );

        var result = await handler.Handle(query, cancellationToken);

        return (TResult)result!;
    }
}
