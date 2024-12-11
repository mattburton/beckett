using Beckett.Database;
using Beckett.Subscriptions.Queries;

namespace Beckett.Subscriptions.Retries;

public class RetryClient(IPostgresDatabase database, PostgresOptions options) : IRetryClient
{
    public Task BulkRetry(long[] ids, CancellationToken cancellationToken)
    {
        return database.Execute(new ScheduleCheckpoints(ids, DateTimeOffset.UtcNow, options), cancellationToken);
    }

    public Task BulkSkip(long[] ids, CancellationToken cancellationToken)
    {
        return database.Execute(new SkipCheckpointPositions(ids, options), cancellationToken);
    }

    public Task Retry(long id, CancellationToken cancellationToken)
    {
        return database.Execute(new ScheduleCheckpoints([id], DateTimeOffset.UtcNow, options), cancellationToken);
    }

    public Task Skip(long id, CancellationToken cancellationToken)
    {
        return database.Execute(new SkipCheckpointPosition(id, options), cancellationToken);
    }
}
