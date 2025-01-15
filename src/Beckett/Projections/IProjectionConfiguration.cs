using System.Text;

namespace Beckett.Projections;

public interface IProjectionConfiguration<TKey>
{
    IProjectionMessageConfiguration<TKey> CreatedBy<TMessage>(Func<TMessage, TKey> key);
    IProjectionMessageConfiguration<TKey> CreatedOrUpdatedBy<TMessage>(Func<TMessage, TKey> key);
    IProjectionMessageConfiguration<TKey> UpdatedBy<TMessage>(Func<TMessage, TKey> key);
    IProjectionMessageConfiguration<TKey> DeletedBy<TMessage>(Func<TMessage, TKey> key);
}

public class ProjectionConfiguration<TKey> : IProjectionConfiguration<TKey>
{
    private readonly Dictionary<Type, ProjectionMessageConfiguration<TKey>> _map = new();

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

    public IReadOnlyList<Type> GetMessageTypes() => _map.Keys.ToArray();

    public void Validate<TState>(TState state)
    {
        if (state is not IApplyDiagnostics diagnostics)
        {
            return;
        }

        var configuredMessageTypes = GetMessageTypesExcludingDeletedBy().ToArray();
        var appliedMessageTypes = diagnostics.AppliedMessageTypes;

        if (configuredMessageTypes.Length == appliedMessageTypes.Length)
        {
            return;
        }

        var errorMessage = new StringBuilder();

        var appliedButNotConfigured = appliedMessageTypes.Except(configuredMessageTypes).ToArray();

        if (appliedButNotConfigured.Length > 0)
        {
            errorMessage.AppendLine("Applied but not configured:");

            foreach (var eventType in appliedButNotConfigured)
            {
                errorMessage.AppendLine(eventType.FullName);
            }
        }

        var configuredButNotApplied = configuredMessageTypes.Except(appliedMessageTypes).ToArray();

        if (configuredButNotApplied.Length > 0)
        {
            errorMessage.AppendLine("Configured but not applied:");

            foreach (var eventType in configuredButNotApplied)
            {
                errorMessage.AppendLine(eventType.FullName);
            }
        }

        var message = $"{typeof(TState).Name} projection is not configured correctly:\n\n{errorMessage}";

        throw new Exception(message);
    }

    public IReadOnlyList<IMessageContext> Filter(IReadOnlyList<IMessageContext> batch)
    {
        return batch.Where(x => x.MessageType != null && _map.ContainsKey(x.MessageType)).ToList();
    }

    public ProjectionMessageConfiguration<TKey> GetConfigurationFor(Type messageType) => _map[messageType];

    private Type[] GetMessageTypesExcludingDeletedBy() =>
        _map.Where(x => x.Value.Action != ProjectionAction.Delete).Select(x => x.Key).ToArray();

    private ProjectionMessageConfiguration<TKey> RegisterMessageConfiguration<TMessage>(
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
