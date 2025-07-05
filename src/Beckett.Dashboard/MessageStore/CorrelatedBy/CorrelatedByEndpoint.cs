using Beckett.Database;

namespace Beckett.Dashboard.MessageStore.CorrelatedBy;

public static class CorrelatedByEndpoint
{
    public static async Task<IResult> Handle(
        string correlationId,
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
            new CorrelatedMessagesQuery(correlationId, query, offset, pageSizeParameter),
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
