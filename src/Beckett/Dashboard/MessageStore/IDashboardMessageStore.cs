namespace Beckett.Dashboard.MessageStore;

public interface IDashboardMessageStore
{
    Task<GetCategoriesResult> GetCategories(
        string? query,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    );

    Task<GetCategoryStreamsResult> GetCategoryStreams(
        string category,
        string? query,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    );

    Task<GetCorrelatedMessagesResult> GetCorrelatedMessages(
        string correlationId,
        string? query,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    );

    Task<GetMessageResult?> GetMessage(string id, CancellationToken cancellationToken);

    Task<GetMessageResult?> GetMessage(string streamName, long streamPosition, CancellationToken cancellationToken);

    Task<GetStreamMessagesResult> GetStreamMessages(
        string streamName,
        string? query,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    );
}
