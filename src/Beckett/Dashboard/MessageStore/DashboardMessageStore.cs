using Beckett.Dashboard.MessageStore.Queries;
using Beckett.Database;

namespace Beckett.Dashboard.MessageStore;

public class DashboardMessageStore(IPostgresDatabase database) : IDashboardMessageStore
{
    public Task<GetCategoriesResult> GetCategories(CancellationToken cancellationToken)
    {
        return database.Execute(new GetCategories(), cancellationToken);
    }

    public Task<GetCategoryStreamsResult> GetCategoryStreams(string category, CancellationToken cancellationToken)
    {
        return database.Execute(new GetCategoryStreams(category), cancellationToken);
    }

    public Task<GetMessageResult?> GetMessage(string id, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(id, out var guid))
        {
            throw new InvalidOperationException("Invalid message ID");
        }

        return database.Execute(new GetMessage(guid), cancellationToken);
    }

    public Task<GetStreamMessagesResult> GetStreamMessages(string streamName, CancellationToken cancellationToken)
    {
        return database.Execute(new GetStreamMessages(streamName), cancellationToken);
    }
}
