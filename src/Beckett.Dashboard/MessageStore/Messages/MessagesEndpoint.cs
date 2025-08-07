using Beckett.Database;

namespace Beckett.Dashboard.MessageStore.Messages;

public static class MessagesEndpoint
{
    public static async Task<IResult> Handle(
        string category,
        string streamName,
        string? query,
        int? page,
        int? pageSize,
        IPostgresDatabase database,
        PostgresOptions options,
        CancellationToken cancellationToken
    )
    {
        var decodedStreamName = HttpUtility.UrlDecode(streamName);
        var pageParameter = page.ToPageParameter();
        var pageSizeParameter = pageSize.ToPageSizeParameter();
        var offset = Pagination.ToOffset(pageParameter, pageSizeParameter);

        var result = await database.Execute(
            new MessagesQuery(decodedStreamName, query, offset, pageSizeParameter),
            cancellationToken
        );

        return Results.Extensions.Render<Messages>(
            new Messages.ViewModel(
                category,
                decodedStreamName,
                query,
                result.Messages,
                pageParameter,
                pageSizeParameter,
                result.TotalResults
            )
        );
    }
}
