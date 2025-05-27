using Beckett.Database;

namespace Beckett.Dashboard.Subscriptions.Checkpoints.Lagging;

public static class LaggingEndpoint
{
    public static async Task<IResult> Handle(
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
            new LaggingQuery(offset, pageSizeParameter, options),
            cancellationToken
        );

        return Results.Extensions.Render<Lagging>(
            new Lagging.ViewModel(result.Subscriptions, null, pageParameter, pageSizeParameter, result.TotalResults)
        );
    }
}
