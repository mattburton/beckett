namespace Beckett.Subscriptions.Retries;

public static class RetryQueues
{
    public const string BulkRetryQueue = "$bulk_retry_queue";
    public const string BulkDeleteQueue = "$bulk_delete_queue";
}
