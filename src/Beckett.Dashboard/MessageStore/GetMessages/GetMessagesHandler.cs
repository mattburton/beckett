namespace Beckett.Dashboard.MessageStore.GetMessages;

public static class GetMessagesHandler
{
    public static async Task<IResult> Get(
        string category,
        string streamName,
        string? query,
        int? page,
        int? pageSize,
        IDashboard dashboard,
        CancellationToken cancellationToken
    )
    {
        var decodedStreamName = HttpUtility.UrlDecode(streamName);
        var pageParameter = page.ToPageParameter();
        var pageSizeParameter = pageSize.ToPageSizeParameter();

        var result = await dashboard.MessageStore.GetStreamMessages(
            decodedStreamName,
            query,
            pageParameter,
            pageSizeParameter,
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
