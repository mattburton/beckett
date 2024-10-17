using System.Web;

namespace Beckett.Dashboard.MessageStore;

public static class MessagesPage
{
    public static RouteGroupBuilder MessagesRoute(this RouteGroupBuilder builder)
    {
        builder.MapGet("/message-store/categories/{category}/{streamName}", Handler);

        return builder;
    }

    public static async Task<IResult> Handler(
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

        var result = await dashboard.MessageStore.GetStreamMessages(
            decodedStreamName,
            query,
            page.ToPage(),
            pageSize.ToPageSize(),
            cancellationToken
        );

        return new Messages(
            new ViewModel(
                category,
                decodedStreamName,
                query,
                result.Messages,
                page.ToPage(),
                pageSize.ToPageSize(),
                result.TotalResults
            )
        );
    }

    public record ViewModel(
        string Category,
        string StreamName,
        string? Query,
        IReadOnlyList<GetStreamMessagesResult.Message> Messages,
        int Page,
        int PageSize,
        int TotalResults
    ) : IPagedViewModel;
}
