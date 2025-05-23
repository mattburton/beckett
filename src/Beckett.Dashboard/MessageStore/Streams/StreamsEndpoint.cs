using Beckett.Dashboard.MessageStore.Components;

namespace Beckett.Dashboard.MessageStore.Streams;

public static class StreamsEndpoint
{
    public static async Task<IResult> Handle(
        HttpContext context,
        string category,
        string? query,
        int? page,
        int? pageSize,
        IDashboard dashboard,
        CancellationToken cancellationToken
    )
    {
        var tenant = TenantFilter.GetCurrentTenant(context);
        var decodedCategory = HttpUtility.UrlDecode(category);
        var pageParameter = page.ToPageParameter();
        var pageSizeParameter = pageSize.ToPageSizeParameter();

        var result = await dashboard.MessageStore.GetCategoryStreams(
            tenant,
            decodedCategory,
            query,
            pageParameter,
            pageSizeParameter,
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
