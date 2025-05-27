using Beckett.Database;

namespace Beckett.Dashboard.Subscriptions.Checkpoints.Failed;

public static class FailedEndpoint
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
            new FailedQuery(query, offset, pageSizeParameter, options),
            cancellationToken
        );

        return Results.Extensions.Render<Failed>(
            new Failed.ViewModel(
                result.Failures,
                query,
                pageParameter,
                pageSizeParameter,
                result.TotalResults
            )
        );
    }
}
