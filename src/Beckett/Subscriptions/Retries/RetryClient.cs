using Beckett.Database;
using Beckett.Subscriptions.Retries.Queries;

namespace Beckett.Subscriptions.Retries;

public class RetryClient(IPostgresDatabase database) : IRetryClient
{
    public async Task RequestManualRetry(Guid id, CancellationToken cancellationToken)
    {
        await database.Execute(
            new RecordRetryEvent(id, RetryStatus.ManualRetryRequested, retryAt: DateTimeOffset.UtcNow),
            cancellationToken
        );
    }

    public async Task DeleteRetry(Guid id, CancellationToken cancellationToken)
    {
        await database.Execute(
            new RecordRetryEvent(id, RetryStatus.Deleted),
            cancellationToken
        );
    }
}
