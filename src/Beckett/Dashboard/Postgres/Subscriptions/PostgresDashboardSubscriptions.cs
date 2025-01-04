using Beckett.Dashboard.Postgres.Subscriptions.Queries;
using Beckett.Database;
using Beckett.Subscriptions.Queries;

namespace Beckett.Dashboard.Postgres.Subscriptions;

public class PostgresDashboardSubscriptions(
    IPostgresDatabase database,
    PostgresOptions options
) : IDashboardSubscriptions
{
    public Task<GetSubscriptionsResult> GetSubscriptions(string? query, int page, int pageSize, CancellationToken cancellationToken)
    {
        var offset = Pagination.ToOffset(page, pageSize);

        return database.Execute(new GetSubscriptions(query, offset, pageSize, options), cancellationToken);
    }

    public Task<GetSubscriptionResult?> GetSubscription(
        string groupName,
        string name,
        CancellationToken cancellationToken
    )
    {
        return database.Execute(new GetSubscription(groupName, name, options), cancellationToken);
    }

    public Task<GetLaggingSubscriptionsResult> GetLaggingSubscriptions(
        int page,
        int pageSize,
        CancellationToken cancellationToken
    )
    {
        var offset = Pagination.ToOffset(page, pageSize);

        return database.Execute(new GetLaggingSubscriptions(offset, pageSize, options), cancellationToken);
    }

    public Task<GetReservationsResult> GetReservations(
        string? query,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    )
    {
        var offset = Pagination.ToOffset(page, pageSize);

        return database.Execute(new GetReservations(query, offset, pageSize, options), cancellationToken);
    }

    public Task<GetRetriesResult> GetRetries(string? query, int page, int pageSize, CancellationToken cancellationToken)
    {
        var offset = Pagination.ToOffset(page, pageSize);

        return database.Execute(new GetRetries(query, offset, pageSize, options), cancellationToken);
    }

    public Task<GetCheckpointResult?> GetCheckpoint(long id, CancellationToken cancellationToken)
    {
        return database.Execute(new GetCheckpoint(id, options), cancellationToken);
    }

    public Task<GetFailedResult> GetFailed(string? query, int page, int pageSize, CancellationToken cancellationToken)
    {
        var offset = Pagination.ToOffset(page, pageSize);

        return database.Execute(new GetFailed(query, offset, pageSize, options), cancellationToken);
    }

    public Task ReleaseCheckpointReservation(long id, CancellationToken cancellationToken)
    {
        return database.Execute(new ReleaseCheckpointReservation(id, options), cancellationToken);
    }

    public Task ResetSubscription(string groupName, string name, CancellationToken cancellationToken)
    {
        return database.Execute(new ResetSubscription(groupName, name, options), cancellationToken);
    }
}
