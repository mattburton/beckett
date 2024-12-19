namespace Beckett.Projections;

public interface IProjectionConfiguration<TKey>
{
    IProjectionMessageConfiguration<TKey> CreatedBy<TMessage>(Func<TMessage, TKey> key);
    IProjectionMessageConfiguration<TKey> CreatedOrUpdatedBy<TMessage>(Func<TMessage, TKey> key);
    IProjectionMessageConfiguration<TKey> UpdatedBy<TMessage>(Func<TMessage, TKey> key);
    IProjectionMessageConfiguration<TKey> DeletedBy<TMessage>(Func<TMessage, TKey> key);
}

public enum ProjectionAction
{
    Create,
    CreateOrUpdate,
    Update,
    Delete
}

public class ProjectionConfiguration<TKey> : IProjectionConfiguration<TKey>
{
    private readonly Dictionary<Type, IProjectionMessageConfiguration<TKey>> _map = new();

    public IProjectionMessageConfiguration<TKey> CreatedBy<TMessage>(Func<TMessage, TKey> key)
    {
        return RegisterMessageConfiguration(ProjectionAction.Create, key);
    }

    public IProjectionMessageConfiguration<TKey> CreatedOrUpdatedBy<TMessage>(Func<TMessage, TKey> key)
    {
        return RegisterMessageConfiguration(ProjectionAction.CreateOrUpdate, key);
    }

    public IProjectionMessageConfiguration<TKey> UpdatedBy<TMessage>(Func<TMessage, TKey> key)
    {
        return RegisterMessageConfiguration(ProjectionAction.Update, key);
    }

    public IProjectionMessageConfiguration<TKey> DeletedBy<TMessage>(Func<TMessage, TKey> key)
    {
        return RegisterMessageConfiguration(ProjectionAction.Delete, key);
    }

    private IProjectionMessageConfiguration<TKey> RegisterMessageConfiguration<TMessage>(
        ProjectionAction action,
        Func<TMessage, TKey> key
    )
    {
        var messageType = typeof(TMessage);

        var messageConfiguration = ProjectionMessageConfiguration<TKey>.Create(action, key);

        _map.TryAdd(messageType, messageConfiguration);

        return messageConfiguration;
    }
}

public interface IProjectionMessageConfiguration<TKey>
{
    IProjectionMessageConfiguration<TKey> WithFilter<TMessage>(Predicate<TMessage> filter);
    IProjectionMessageConfiguration<TKey> IgnoreWhenNotFound();
}

public class ProjectionMessageConfiguration<TKey>(
    Type messageType,
    ProjectionAction action,
    Func<object, TKey> key
) : IProjectionMessageConfiguration<TKey>
{
    internal Type MessageType { get; } = messageType;
    internal ProjectionAction Action { get; } = action;
    internal Func<object, TKey> Key { get; } = key;
    internal Predicate<object>? Filter { get; private set; }
    internal bool IgnoreIfNotFound { get; private set; }

    internal static IProjectionMessageConfiguration<TKey> Create<TMessage>(
        ProjectionAction action,
        Func<TMessage, TKey> keySelector
    )
    {
        return new ProjectionMessageConfiguration<TKey>(typeof(TMessage), action, x => keySelector((TMessage)x));
    }

    public IProjectionMessageConfiguration<TKey> WithFilter<TMessage>(Predicate<TMessage> filter)
    {
        Filter = x => filter((TMessage)x);

        return this;
    }

    public IProjectionMessageConfiguration<TKey> IgnoreWhenNotFound()
    {
        IgnoreIfNotFound = true;

        return this;
    }
}

public interface IProjection<T, TKey> : IMessageBatchHandler where T : IApply, new()
{
    void Configure(IProjectionConfiguration<TKey> configuration);
    void Apply(T model, IMessageBatch batch);
    Task<T?> Load(TKey key, CancellationToken cancellationToken);
    Task Create(T model, CancellationToken cancellationToken);
    Task Update(T model, CancellationToken cancellationToken);
    Task Delete(T model, CancellationToken cancellationToken);

    async Task IMessageBatchHandler.Handle(IMessageBatch batch, CancellationToken cancellationToken)
    {

    }
}

public abstract class Projection<T, TKey> where T : IApply, new()
{
}
