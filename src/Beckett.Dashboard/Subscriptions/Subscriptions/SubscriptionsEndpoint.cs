using Beckett.Database;

namespace Beckett.Dashboard.Subscriptions.Subscriptions;

public static class SubscriptionsEndpoint
{
    public static async Task<IResult> Handle(
        string? query,
        int? page,
        int? pageSize,
        IPostgresDatabase database,
        PostgresOptions options,
        CancellationToken cancellationToken
    )
    {
        var pageParameter = page.ToPageParameter();
        var pageSizeParameter = pageSize.ToPageSizeParameter();
        var offset = Pagination.ToOffset(pageParameter, pageSizeParameter);

        var result = await database.Execute(
            new SubscriptionsQuery(query, offset, pageSizeParameter, options),
            cancellationToken
        );

        return Results.Extensions.Render<Subscriptions>(
            new Subscriptions.ViewModel(
                result.Subscriptions,
                query,
                pageParameter,
                pageSizeParameter,
                result.TotalResults
            )
        );
    }
}
