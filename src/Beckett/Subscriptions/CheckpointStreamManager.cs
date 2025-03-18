using Beckett.Database;
using Beckett.Database.Types;
using Beckett.Subscriptions.Queries;

namespace Beckett.Subscriptions;

public class CheckpointStreamManager(IPostgresDatabase database, PostgresOptions options) : ICheckpointStreamManager
{
    public async Task<IReadOnlyList<string>> GetBlockedStreams(
        long checkpointId,
        string[] streamNames,
        CancellationToken cancellationToken
    )
    {
        return await database.Execute(
            new GetCheckpointBlockedStreams(checkpointId, streamNames, options),
            cancellationToken
        );
    }

    public async Task RetryStreamErrors(
        long checkpointId,
        IEnumerable<CheckpointStreamError> streamErrors,
        CancellationToken cancellationToken
    )
    {
        var retries = streamErrors.Select(CheckpointStreamRetryType.From).ToArray();

        if (retries.Length == 0)
        {
            return;
        }

        await database.Execute(new RecordCheckpointStreamRetries(checkpointId, retries, options), cancellationToken);
    }
}
