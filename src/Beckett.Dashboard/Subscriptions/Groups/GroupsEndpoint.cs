using Beckett.Database;

namespace Beckett.Dashboard.Subscriptions.Groups;

public static class GroupsEndpoint
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
            new GroupsQuery(query, offset, pageSizeParameter, options),
            cancellationToken
        );

        return Results.Extensions.Render<Groups>(
            new Groups.ViewModel(
                result.Groups,
                query,
                pageParameter,
                pageSizeParameter,
                result.TotalResults
            )
        );
    }
}
