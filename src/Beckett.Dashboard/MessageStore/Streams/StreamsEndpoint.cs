using Beckett.Dashboard.MessageStore.Components;
using Beckett.Database;

namespace Beckett.Dashboard.MessageStore.Streams;

public static class StreamsEndpoint
{
    public static async Task<IResult> Handle(
        HttpContext context,
        string category,
        string? query,
        int? page,
        int? pageSize,
        IPostgresDatabase database,
        PostgresOptions options,
        CancellationToken cancellationToken
    )
    {
        var tenant = TenantFilter.GetCurrentTenant(context);
        var decodedCategory = HttpUtility.UrlDecode(category);
        var pageParameter = page.ToPageParameter();
        var pageSizeParameter = pageSize.ToPageSizeParameter();
        var offset = Pagination.ToOffset(pageParameter, pageSizeParameter);

        var result = await database.Execute(
            new StreamsQuery(tenant, decodedCategory, query, offset, pageSizeParameter),
            cancellationToken
        );

        return Results.Extensions.Render<Streams>(
            new Streams.ViewModel(
                decodedCategory,
                result.Streams,
                query,
                page.ToPageParameter(),
                pageSize.ToPageSizeParameter(),
                result.TotalResults
            )
        );
    }
}
