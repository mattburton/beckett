namespace Core.Contracts;

public interface IExternalEvent : IEventType, IHavePartitionKey;

public interface IEventType : IHaveTypeName, IProcessorInput;

public interface IHaveTypeName
{
    string TypeName() => GetType().Name;
}

public interface IHavePartitionKey
{
    string PartitionKey();
}

public interface IProcessorInput;
