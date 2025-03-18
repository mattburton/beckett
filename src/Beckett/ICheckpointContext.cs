namespace Beckett;

public interface ICheckpointContext
{
    /// <summary>
    /// The checkpoint ID
    /// </summary>
    long Id { get; }

    /// <summary>
    /// Parent ID of the checkpoint. One use case for child checkpoints is when a subscription is global scoped but
    /// manages retries and failures per-stream.
    /// </summary>
    long? ParentId { get; }

    /// <summary>
    /// Subscription group name for the checkpoint
    /// </summary>
    string GroupName { get; }

    /// <summary>
    /// Subscription name for the checkpoint
    /// </summary>
    string SubscriptionName { get; }

    /// <summary>
    /// Stream name of the checkpoint
    /// </summary>
    string StreamName { get; }

    /// <summary>
    /// The current version of the stream the checkpoint is tracking.
    /// </summary>
    long StreamVersion { get; }

    /// <summary>
    /// The current position of the checkpoint within the stream it is tracking.
    /// </summary>
    long StreamPosition { get; }
}
