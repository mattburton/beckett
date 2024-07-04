namespace Beckett.Dashboard.MessageStore;

public interface IDashboardMessageStore
{
    Task<GetCategoriesResult> GetCategories(CancellationToken cancellationToken);

    Task<GetCategoryStreamsResult> GetCategoryStreams(string category, CancellationToken cancellationToken);

    Task<GetMessageResult?> GetMessage(string id, CancellationToken cancellationToken);

    Task<GetStreamMessagesResult> GetStreamMessages(string streamName, CancellationToken cancellationToken);
}
