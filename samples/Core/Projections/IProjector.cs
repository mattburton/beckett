using Beckett;
using Core.State;

namespace Core.Projections;

public interface IProjector<TState> where TState : class, IStateView, new()
{
    Task<ProjectorResult<TState>> Project(IMessageContext context, CancellationToken cancellationToken);

    Task<ProjectorResult<TState>> Project(IReadOnlyList<IMessageContext> batch, CancellationToken cancellationToken);
}

public record ProjectorResult<TState>(IReadOnlyList<TState> AddedOrUpdated, IReadOnlyList<TState> Removed)
{
    public IReadOnlyList<TState> Changed => AddedOrUpdated.Concat(Removed).ToList();

    public static ProjectorResult<TState> Empty { get; } = new(new List<TState>(), new List<TState>());
}

public class Projector<TState>(IProjection<TState> projection)
    : IProjector<TState> where TState : class, IStateView, new()
{
    public async Task<ProjectorResult<TState>> Project(IMessageContext context, CancellationToken cancellationToken)
    {
        return await Project([context], cancellationToken);
    }

    public async Task<ProjectorResult<TState>> Project(
        IReadOnlyList<IMessageContext> batch,
        CancellationToken cancellationToken
    )
    {
        var configuration = new ProjectionConfiguration();

        projection.Configure(configuration);

        var messagesToApply = configuration.Filter(batch);

        if (messagesToApply.Count == 0)
        {
            return ProjectorResult<TState>.Empty;
        }

        var keyBatches = messagesToApply.GroupBy(x => configuration.GetKey(x))
            .ToDictionary(key => key.Key, key => key.ToList());
        var keys = keyBatches.Select(x => x.Key);
        var addedOrUpdated = new List<TState>();
        var removed = new List<TState>();

        var records = await projection.Load(keys, cancellationToken);

        foreach (var keyBatch in keyBatches)
        {
            foreach (var message in keyBatch.Value)
            {
                var messageConfiguration = configuration.GetConfigurationFor(message.MessageType!);
                var key = messageConfiguration.GetKey(message.Message!);
                var state = records.FirstOrDefault(x => Equals(projection.GetKey(x), key));

                switch (messageConfiguration.Action)
                {
                    case ProjectionAction.Create:
                        state = new TState();
                        break;
                    case ProjectionAction.CreateOrUpdate:
                        state ??= new TState();
                        break;
                    case ProjectionAction.Update:
                        if (state == null)
                        {
                            if (!messageConfiguration.IgnoreWhenNotFound)
                            {
                                throw new InvalidOperationException(
                                    $"Cannot update {typeof(TState).Name} with key {key} - projection not found"
                                );
                            }

                            state = new TState();
                        }

                        break;
                    case ProjectionAction.Delete:
                        if (state != null)
                        {
                            projection.Delete(state);

                            removed.Add(state);
                        }

                        continue;
                    default:
                        throw new InvalidOperationException(
                            "Unhandled projection action: " + messageConfiguration.Action
                        );
                }

                state.Apply(message);

                projection.Save(state);

                addedOrUpdated.Add(state);
            }
        }

        await projection.SaveChanges(cancellationToken);

        return new ProjectorResult<TState>(addedOrUpdated, removed);
    }
}
