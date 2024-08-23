using Beckett.Subscriptions.Retries;

namespace Beckett.Subscriptions;

public class SubscriptionOptions
{
    public string GroupName { get; set; } = "default";
    public bool Enabled { get; set; }
    public int Concurrency { get; set; } = Math.Min(Environment.ProcessorCount * 5, 20);
    public int GlobalStreamBatchSize { get; set; } = 500;
    public TimeSpan GlobalStreamPollingInterval { get; set; } = TimeSpan.FromSeconds(10);
    public int InitializationBatchSize { get; set; } = 1000;
    public TimeSpan ReservationTimeout { get; set; } = TimeSpan.FromMinutes(5);
    public int ReservationRecoveryBatchSize { get; set; } = 100;
    public TimeSpan ReservationRecoveryInterval { get; set; } = TimeSpan.FromMinutes(1);
    public int SubscriptionStreamBatchSize { get; set; } = 500;
    public TimeSpan SubscriptionStreamPollingInterval { get; set; } = TimeSpan.FromSeconds(10);
    internal Dictionary<Type, int> MaxRetriesByExceptionType { get; } = new() {{typeof(Exception), 10}};

    public RetryOptions Retries { get; set; } = new();

    public void MaxRetryCount<TException>(int maxRetryCount) where TException : Exception
    {
        MaxRetriesByExceptionType[typeof(TException)] = maxRetryCount;
    }
}
