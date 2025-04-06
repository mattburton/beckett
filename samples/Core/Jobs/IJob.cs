using Core.Contracts;

namespace Core.Jobs;

public interface IJob : IHaveTypeName, IHavePartitionKey, IProcessorInput;
