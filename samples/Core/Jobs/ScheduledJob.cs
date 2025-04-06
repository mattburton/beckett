using Core.Modules;

namespace Core.Jobs;

public record ScheduledJob(IJob Job, TimeSpan Delay) : IJob, IShouldNotBeMappedAutomatically
{
    public string PartitionKey() => Job.PartitionKey();
}
