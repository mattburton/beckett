using Beckett.Dashboard.MessageStore.Queries;
using Beckett.Database;

namespace Beckett.Dashboard.MessageStore;

public class DashboardMessageStore(IPostgresDatabase database) : IDashboardMessageStore
{
    public Task<GetCategoriesResult> GetCategories(string? query, CancellationToken cancellationToken)
    {
        return database.Execute(new GetCategories(query), cancellationToken);
    }

    public Task<GetCategoryStreamsResult> GetCategoryStreams(string category, string? query, CancellationToken cancellationToken)
    {
        return database.Execute(new GetCategoryStreams(category, query), cancellationToken);
    }

    public Task<GetMessageResult?> GetMessage(string id, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(id, out var guid))
        {
            throw new InvalidOperationException("Invalid message ID");
        }

        return database.Execute(new GetMessage(guid), cancellationToken);
    }

    public Task<GetMessageResult?> GetMessage(
        string streamName,
        long streamPosition,
        CancellationToken cancellationToken
    )
    {
        return database.Execute(new GetMessageByStreamPosition(streamName, streamPosition), cancellationToken);
    }

    public Task<GetStreamMessagesResult> GetStreamMessages(string streamName, string? query, CancellationToken cancellationToken)
    {
        return database.Execute(new GetStreamMessages(streamName, query), cancellationToken);
    }
}
