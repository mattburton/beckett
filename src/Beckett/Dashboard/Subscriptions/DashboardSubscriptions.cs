using Beckett.Dashboard.Subscriptions.Queries;
using Beckett.Database;
using Beckett.Subscriptions.Retries;

namespace Beckett.Dashboard.Subscriptions;

public class DashboardSubscriptions(IPostgresDatabase database, IMessageStore messageStore) : IDashboardSubscriptions
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

    public async Task<GetRetryDetailsResult?> GetRetryDetails(Guid id, CancellationToken cancellationToken)
    {
        var stream = await messageStore.ReadStream(RetryStreamName.For(id), cancellationToken);

        return stream.IsEmpty ? null : stream.ProjectTo<GetRetryDetailsResult>();
    }

    public Task<GetFailedResult> GetFailed(CancellationToken cancellationToken)
    {
        return database.Execute(new GetFailed(), cancellationToken);
    }
}
