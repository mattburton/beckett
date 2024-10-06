using Beckett.Dashboard.Subscriptions.Queries;
using Beckett.Database;
using Beckett.Subscriptions.Retries;

namespace Beckett.Dashboard.Subscriptions;

public class DashboardSubscriptions(
    IPostgresDatabase database,
    IMessageStore messageStore,
    PostgresOptions options
) : IDashboardSubscriptions
{
    public Task<GetSubscriptionsResult> GetSubscriptions(CancellationToken cancellationToken)
    {
        return database.Execute(new GetSubscriptions(options), cancellationToken);
    }

    public Task<GetLaggingSubscriptionsResult> GetLaggingSubscriptions(CancellationToken cancellationToken)
    {
        return database.Execute(new GetLaggingSubscriptions(options), cancellationToken);
    }

    public Task<GetRetriesResult> GetRetries(CancellationToken cancellationToken)
    {
        return database.Execute(new GetRetries(options), cancellationToken);
    }

    public async Task<GetRetryDetailsResult?> GetRetryDetails(Guid id, CancellationToken cancellationToken)
    {
        var stream = await messageStore.ReadStream(RetryStreamName.For(id), cancellationToken);

        return stream.IsEmpty ? null : stream.ProjectTo<GetRetryDetailsResult>();
    }

    public Task<GetFailedResult> GetFailed(CancellationToken cancellationToken)
    {
        return database.Execute(new GetFailed(options), cancellationToken);
    }
}
