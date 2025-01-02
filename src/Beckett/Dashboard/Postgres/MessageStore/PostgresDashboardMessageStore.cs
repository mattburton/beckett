using Beckett.Dashboard.Postgres.MessageStore.Queries;
using Beckett.Database;
using Beckett.Database.Queries;

namespace Beckett.Dashboard.Postgres.MessageStore;

public class PostgresDashboardMessageStore(
    IPostgresDataSource dataSource,
    IPostgresDatabase database,
    PostgresOptions options
) : IDashboardMessageStore
{
    public async Task<GetTenantsResult> GetTenants(CancellationToken cancellationToken)
    {
        var results = await database.Execute(new GetTenants(options), cancellationToken);

        if (results.Tenants.Count == 0)
        {
            results.Tenants.Add(BeckettContext.DefaultTenant);
        }

        return results;
    }

    public async Task RefreshTenants(CancellationToken cancellationToken)
    {
        const string key = "Beckett:RefreshTenantMaterializedView";

        await using var connection = dataSource.CreateConnection();

        await connection.OpenAsync(cancellationToken);

        var locked = await database.Execute(new TryAdvisoryLock(key, options), connection, cancellationToken);

        if (!locked)
        {
            return;
        }

        await database.Execute(new RefreshTenants(options), connection, cancellationToken);

        await database.Execute(new AdvisoryUnlock(key, options), connection, cancellationToken);
    }

    public Task<GetCategoriesResult> GetCategories(
        string tenant,
        string? query,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    )
    {
        var offset = Pagination.ToOffset(page, pageSize);

        return database.Execute(new GetCategories(tenant, query, offset, pageSize, options), cancellationToken);
    }

    public Task<GetCategoryStreamsResult> GetCategoryStreams(
        string tenant,
        string category,
        string? query,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    )
    {
        var offset = Pagination.ToOffset(page, pageSize);

        return database.Execute(
            new GetCategoryStreams(tenant, category, query, offset, pageSize, options),
            cancellationToken
        );
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
