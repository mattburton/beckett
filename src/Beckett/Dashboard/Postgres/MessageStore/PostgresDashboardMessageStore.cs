using Beckett.Dashboard.Postgres.MessageStore.Queries;
using Beckett.Database;

namespace Beckett.Dashboard.Postgres.MessageStore;

public class PostgresDashboardMessageStore(IPostgresDatabase database, PostgresOptions options) : IDashboardMessageStore
{
    public Task<GetCategoriesResult> GetCategories(
        string? query,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    )
    {
        var offset = Pagination.ToOffset(page, pageSize);

        return database.Execute(new GetCategories(query, offset, pageSize, options), cancellationToken);
    }

    public Task<GetCategoryStreamsResult> GetCategoryStreams(
        string category,
        string? query,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    )
    {
        var offset = Pagination.ToOffset(page, pageSize);

        return database.Execute(new GetCategoryStreams(category, query, offset, pageSize, options), cancellationToken);
    }

    public Task<GetCorrelatedMessagesResult> GetCorrelatedMessages(
        string correlationId,
        string? query,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    )
    {
        var offset = Pagination.ToOffset(page, pageSize);

        return database.Execute(
            new GetCorrelatedMessages(correlationId, query, offset, pageSize, options),
            cancellationToken
        );
    }

    public Task<GetMessageResult?> GetMessage(string id, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(id, out var guid))
        {
            throw new InvalidOperationException("Invalid message ID");
        }

        return database.Execute(new GetMessage(guid, options), cancellationToken);
    }

    public Task<GetMessageResult?> GetMessage(
        string streamName,
        long streamPosition,
        CancellationToken cancellationToken
    )
    {
        return database.Execute(new GetMessageByStreamPosition(streamName, streamPosition, options), cancellationToken);
    }

    public Task<GetStreamMessagesResult> GetStreamMessages(
        string streamName,
        string? query,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    )
    {
        var offset = Pagination.ToOffset(page, pageSize);

        return database.Execute(new GetStreamMessages(streamName, query, offset, pageSize, options), cancellationToken);
    }
}
