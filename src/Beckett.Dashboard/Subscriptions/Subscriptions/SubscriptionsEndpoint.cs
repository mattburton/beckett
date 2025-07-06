using Beckett.Database;

namespace Beckett.Dashboard.Subscriptions.Subscriptions;

public static class SubscriptionsEndpoint
{
    public static async Task<IResult> Handle(
        string groupName,
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
            new SubscriptionsQuery(groupName, query, offset, pageSizeParameter),
            cancellationToken
        );

        return Results.Extensions.Render<Subscriptions>(
            new Subscriptions.ViewModel(
                groupName,
                result.Subscriptions,
                query,
                pageParameter,
                pageSizeParameter,
                result.TotalResults
            )
        );
    }
}
