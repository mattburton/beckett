using Core.Contracts;
using Core.Modules;

namespace Core.Processors;

public record ScheduledJob(IJob Job, TimeSpan Delay) : IJob, IShouldNotBeMappedAutomatically
{
    public string PartitionKey() => Job.PartitionKey();
}
