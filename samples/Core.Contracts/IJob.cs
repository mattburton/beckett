namespace Core.Contracts;

public interface IJob : IHaveTypeName, IHavePartitionKey, ISupportSubscriptions;
