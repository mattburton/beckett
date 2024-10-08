using Beckett.Database.Types;
using Beckett.Subscriptions;

namespace Beckett.Dashboard.Subscriptions;

public class GetCheckpointResult
{
    public required long Id { get; init; }
    public required string GroupName { get; init; }
    public required string Name { get; init; }
    public required string StreamName { get; init; }
    public required long StreamVersion { get; init; }
    public required long StreamPosition { get; init; }
    public required CheckpointStatus Status { get; init; }
    public DateTimeOffset? ProcessAt { get; init; }
    public required RetryType[] Retries { get; init; }

    public int TotalAttempts => Retries?.Length > 0 ? Retries.Length - 1 : 0;

    public string StreamCategory
    {
        get
        {
            var firstHyphen = StreamName.IndexOf('-');

            return StreamName[..firstHyphen];
        }
    }

    public bool ShowControls => Status switch
    {
        CheckpointStatus.Active => false,
        _ => true
    };
}
