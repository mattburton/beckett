using Beckett.Dashboard.MessageStore.Shared.Components;

namespace Beckett.Dashboard.MessageStore.GetStreams;

public static class GetStreamsHandler
{
    public static async Task<IResult> Get(
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
