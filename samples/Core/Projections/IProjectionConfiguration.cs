using System.Text;
using Beckett;
using Core.State;

namespace Core.Projections;

public interface IProjectionConfiguration
{
    IProjectionMessageConfiguration<TMessage> CreatedBy<TMessage>(Func<TMessage, object> key);
    IProjectionMessageConfiguration<TMessage> CreatedOrUpdatedBy<TMessage>(Func<TMessage, object> key);
    IProjectionMessageConfiguration<TMessage> UpdatedBy<TMessage>(Func<TMessage, object> key);
    IProjectionMessageConfiguration<TMessage> DeletedBy<TMessage>(Func<TMessage, object> key);
}

public class ProjectionConfiguration : IProjectionConfiguration
{
    private readonly Dictionary<Type, ProjectionMessageConfiguration> _map = new();

    public IProjectionMessageConfiguration<TMessage> CreatedBy<TMessage>(Func<TMessage, object> key)
    {
        return RegisterMessageConfiguration(ProjectionAction.Create, key);
    }

    public IProjectionMessageConfiguration<TMessage> CreatedOrUpdatedBy<TMessage>(Func<TMessage, object> key)
    {
        return RegisterMessageConfiguration(ProjectionAction.CreateOrUpdate, key);
    }

    public IProjectionMessageConfiguration<TMessage> UpdatedBy<TMessage>(Func<TMessage, object> key)
    {
        return RegisterMessageConfiguration(ProjectionAction.Update, key);
    }

    public IProjectionMessageConfiguration<TMessage> DeletedBy<TMessage>(Func<TMessage, object> key)
    {
        return RegisterMessageConfiguration(ProjectionAction.Delete, key);
    }

    public IReadOnlyList<Type> GetMessageTypes() => _map.Keys.ToArray();

    public void Validate<TState>(TState state)
    {
        if (state is not IApplyMessageTypes diagnostics)
        {
            return;
        }

        var configuredMessageTypes = GetMessageTypesExcludingDeletedBy().ToArray();
        var appliedMessageTypes = diagnostics.MessageTypes();

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
        return batch.Where(x => x.MessageType != null && _map.ContainsKey(x.MessageType))
            .Where(x => _map[x.MessageType!].WherePredicate(x.Message!)).ToList();
    }

    public object GetKey(IMessageContext context) => GetConfigurationFor(context.MessageType!).Key(context.Message!);

    public ProjectionMessageConfiguration GetConfigurationFor(Type messageType) => _map[messageType];

    private Type[] GetMessageTypesExcludingDeletedBy() =>
        _map.Where(x => x.Value.Action != ProjectionAction.Delete).Select(x => x.Key).ToArray();

    private ProjectionMessageConfiguration<TMessage> RegisterMessageConfiguration<TMessage>(
        ProjectionAction action,
        Func<TMessage, object> key
    )
    {
        var messageType = typeof(TMessage);

        var messageConfiguration = ProjectionMessageConfiguration<TMessage>.Create(action, key);

        _map.TryAdd(messageType, messageConfiguration.Configuration);

        return messageConfiguration;
    }
}
