namespace Beckett.Dashboard.MessageStore;

public static class CorrelatedByPage
{
    public static RouteGroupBuilder CorrelatedByRoute(this RouteGroupBuilder builder)
    {
        builder.MapGet("/message-store/correlated-by/{correlationId}", Handler);

        return builder;
    }

    public static async Task<IResult> Handler(
        string correlationId,
        string? query,
        int? page,
        int? pageSize,
        IDashboard dashboard,
        CancellationToken cancellationToken
    )
    {
        var pageParameter = page.ToPageParameter();
        var pageSizeParameter = pageSize.ToPageSizeParameter();

        var result = await dashboard.MessageStore.GetCorrelatedMessages(
            correlationId,
            query,
            pageParameter,
            pageSizeParameter,
            cancellationToken
        );

        return new CorrelatedBy(
            new ViewModel(
                correlationId,
                query,
                result.Messages,
                pageParameter,
                pageSizeParameter,
                result.TotalResults
            )
        );
    }

    public record ViewModel(
        string CorrelationId,
        string? Query,
        IReadOnlyList<GetCorrelatedMessagesResult.Message> Messages,
        int Page,
        int PageSize,
        int TotalResults
    ) : IPagedViewModel;
}
