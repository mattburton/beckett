using Beckett.Database;

namespace Beckett.Dashboard.Subscriptions.Checkpoints.Retries;

public static class RetriesEndpoint
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
            new RetriesQuery(query, offset, pageSizeParameter),
            cancellationToken
        );

        return Results.Extensions.Render<Retries>(
            new Retries.ViewModel(
                result.Retries,
                query,
                pageParameter,
                pageSizeParameter,
                result.TotalResults
            )
        );
    }
}
