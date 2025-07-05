using Beckett.Database;

namespace Beckett.Dashboard.Scheduled.Messages;

public static class MessagesEndpoint
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
            new MessagesQuery(query, offset, pageSizeParameter),
            cancellationToken
        );

        return Results.Extensions.Render<Messages>(
            new Messages.ViewModel(result.Messages, query, pageParameter, pageSizeParameter, result.TotalResults)
        );
    }
}
