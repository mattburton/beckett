using Beckett.Subscriptions.Retries;

namespace Beckett.Subscriptions;

public class SubscriptionOptions
{
    /// <summary>
    /// Configure the subscription group name for this host. All subscriptions configured in this host will be assigned
    /// to the group and processed by Beckett accordingly. Defaults to "default" if not specified.
    /// </summary>
    public string GroupName { get; set; } = "default";

    /// <summary>
    /// Configure whether subscriptions are enabled for this host. Enabling subscriptions will register all the
    /// necessary dependencies and host services that allows Beckett to process them. Defaults to false.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Configure the number of concurrent subscription streams Beckett can process at one time. Defaults to the value
    /// of the number of CPUs multiplied by 5 or a fixed count of 20, whichever is lower.
    /// </summary>
    public int Concurrency { get; set; } = Math.Min(Environment.ProcessorCount * 5, 20);

    /// <summary>
    /// Configure the batch size when reading the global stream of the message store. New messages read from the global
    /// stream will be dispatched to subscriptions by stream. Defaults to 500.
    /// </summary>
    public int GlobalStreamBatchSize { get; set; } = 500;

    /// <summary>
    /// Configure the polling interval to read new messages from the global stream. When Postgres notifications are
    /// enabled polling is relegated to more of a backup sanity check just to make sure nothing was missed. As such the
    /// default is 10 seconds to minimize the polling overhead while notifications are enabled (also the default). If
    /// desired polling can be disabled entirely by setting the value to <c>TimeSpan.Zero</c>
    /// </summary>
    public TimeSpan GlobalStreamPollingInterval { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Configure the batch size used when initializing subscriptions. This is the number of messages to read per batch
    /// from the global stream while discovering streams and creating checkpoints for them. Setting this to a higher
    /// number allows initialization to proceed more quickly for a large message store, at the expense of additional
    /// overhead reading from it that could affect the performance of the application otherwise. Defaults to 1000.
    /// </summary>
    public int InitializationBatchSize { get; set; } = 1000;

    /// <summary>
    /// Configure the timeout for checkpoint and retry reservations that occur while processing them. When a checkpoint
    /// or retry is available for processing, Beckett reserves it for the requesting consumer. The consumer processes it
    /// and no matter the outcome once complete releases the reservation. If the consumer fails to release the
    /// reservation prior to its reservation timeout (process killed due to scaling in, etc...) then a secondary process
    /// checks for expired reservations on a regular interval - see <see cref="ReservationRecoveryInterval"/> - and
    /// recovers them.
    /// </summary>
    public TimeSpan ReservationTimeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Configure the batch size used when recovering expired reservations. Defaults to 100.
    /// </summary>
    public int ReservationRecoveryBatchSize { get; set; } = 100;

    /// <summary>
    /// Configure the interval to check for expired reservations and recover them. Defaults to 1 minute.
    /// </summary>
    public TimeSpan ReservationRecoveryInterval { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Configure the batch size used when reading new messages from a stream for subscription processing. Ideally
    /// streams should be short, but for longer streams batching becomes necessary. When processing a stream for a
    /// subscription Beckett will keep reading batches of messages from the stream and processing them until there are
    /// none left.
    /// </summary>
    public int SubscriptionStreamBatchSize { get; set; } = 500;

    /// <summary>
    /// Configure the polling interval to check for new subscription streams (checkpoints) to process. When Postgres
    /// notifications are enabled (the default) this interval can be disabled by setting it to <c>TimeSpan.Zero</c> or
    /// increased and used as a backup sanity check to make sure that everything is being processed. Defaults to 10
    /// seconds.
    /// </summary>
    public TimeSpan SubscriptionStreamPollingInterval { get; set; } = TimeSpan.FromSeconds(10);

    internal Dictionary<Type, int> MaxRetriesByExceptionType { get; } = new() {{typeof(Exception), 10}};

    public RetryOptions Retries { get; set; } = new();

    /// <summary>
    /// Configure the default max retry count for all subscriptions registered in this host. This can be overridden by
    /// configuring the max retry count for specific exception types using <see cref="MaxRetryCount{TException}"/>.
    /// </summary>
    /// <param name="maxRetryCount">Max retry count</param>
    public void MaxRetryCount(int maxRetryCount)
    {
        if (maxRetryCount < 0)
        {
            throw new ArgumentException("The max retry count must be greater than or equal to 0", nameof(maxRetryCount));
        }

        MaxRetriesByExceptionType[typeof(Exception)] = maxRetryCount;
    }

    /// <summary>
    /// Configure the max retry count for a specific exception type for all subscriptions registered in this host. This
    /// is useful in scenarios where you have known exceptions that should lead to specific retry behavior - i.e. if a
    /// given exception being thrown means that there is no chance that retrying this subscription will lead to a
    /// successful outcome then we can set the max retry count for that exception type to zero, meaning that it will not
    /// be retried and the status of the checkpoint will be set to failed immediately. In this scenario the failure will
    /// be visible in the list of failed retries on the Beckett dashboard.
    /// </summary>
    /// <param name="maxRetryCount">Max retry count</param>
    /// <typeparam name="TException">Exception type</typeparam>
    public void MaxRetryCount<TException>(int maxRetryCount) where TException : Exception
    {
        if (maxRetryCount < 0)
        {
            throw new ArgumentException("The max retry count must be greater than or equal to 0", nameof(maxRetryCount));
        }

        MaxRetriesByExceptionType[typeof(TException)] = maxRetryCount;
    }
}
