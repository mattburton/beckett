using Beckett.Dashboard.Subscriptions.Queries;
using Beckett.Database;

namespace Beckett.Dashboard.Subscriptions;

public class DashboardSubscriptions(IPostgresDatabase database) : IDashboardSubscriptions
{
    public Task<GetSubscriptionsResult> GetSubscriptions(CancellationToken cancellationToken)
    {
        return database.Execute(new GetSubscriptions(), cancellationToken);
    }

    public Task<GetLaggingSubscriptionsResult> GetLaggingSubscriptions(CancellationToken cancellationToken)
    {
        return database.Execute(new GetLaggingSubscriptions(), cancellationToken);
    }

    public Task<GetRetriesResult> GetRetries(CancellationToken cancellationToken)
    {
        return database.Execute(new GetRetries(), cancellationToken);
    }

    public Task<GetRetryDetailsResult?> GetRetryDetails(Guid id, CancellationToken cancellationToken)
    {
        return database.Execute(new GetRetryDetails(id), cancellationToken);
    }

    public Task<GetFailedResult> GetFailed(CancellationToken cancellationToken)
    {
        return database.Execute(new GetFailed(), cancellationToken);
    }
}
