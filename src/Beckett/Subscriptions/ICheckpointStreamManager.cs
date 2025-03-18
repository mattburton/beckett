namespace Beckett.Subscriptions;

public interface ICheckpointStreamManager
{
    /// <summary>
    /// Get child checkpoint streams that are either retrying or failed for the given checkpoint ID and stream names.
    /// </summary>
    /// <param name="checkpointId"></param>
    /// <param name="streamNames"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<IReadOnlyList<string>> GetBlockedStreams(
        long checkpointId,
        string[] streamNames,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Create child checkpoints to allow individual streams to be retried and the parent checkpoint to continue
    /// processing.
    /// </summary>
    /// <param name="checkpointId"></param>
    /// <param name="streamErrors"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task RetryStreamErrors(
        long checkpointId,
        IEnumerable<CheckpointStreamError> streamErrors,
        CancellationToken cancellationToken
    );
}

public record CheckpointStreamError(string StreamName, long StreamPosition, Exception Exception);
