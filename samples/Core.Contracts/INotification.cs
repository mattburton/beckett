namespace Core.Contracts;

public interface INotification : IHaveTypeName, IHavePartitionKey, ISupportSubscriptions;
