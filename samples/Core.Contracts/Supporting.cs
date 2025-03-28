namespace Core.Contracts;

public interface IHaveTypeName
{
    string TypeName() => GetType().Name;
}

public interface IHavePartitionKey
{
    string PartitionKey();
}

public interface ISupportSubscriptions;
