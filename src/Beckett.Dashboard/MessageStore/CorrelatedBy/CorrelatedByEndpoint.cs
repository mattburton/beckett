namespace Beckett.Dashboard.MessageStore.CorrelatedBy;

public static class CorrelatedByEndpoint
{
    public static async Task<IResult> Handle(
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

        return Results.Extensions.Render<CorrelatedBy>(
            new CorrelatedBy.ViewModel(
                correlationId,
                query,
                result.Messages,
                pageParameter,
                pageSizeParameter,
                result.TotalResults
            )
        );
    }
}
