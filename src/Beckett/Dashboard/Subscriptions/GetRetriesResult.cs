using Beckett.Subscriptions.Retries;

namespace Beckett.Dashboard.Subscriptions;

public record GetRetriesResult(List<GetRetriesResult.Retry> Retries)
{
    public record Retry(
        string GroupName,
        string Name,
        string StreamName,
        long StreamPosition,
        Guid RetryId,
        int Attempts,
        DateTimeOffset? RetryAt,
        RetryStatus Status
    )
    {
        public string RetryIn => DetermineRetryTimeDisplay();

        private string DetermineRetryTimeDisplay()
        {
            if (Status == RetryStatus.Reserved)
            {
                return "In progress";
            }

            return RetryAt == null ? "" : RetryAt.Value.ToRetryTimeDisplay();
        }
    }
}
