using Beckett;
using Core.MessageHandling;

namespace Core.Projections;

public static class ProjectionHandler<TProjection, TState, TKey> where TProjection : IProjection<TState, TKey>
    where TState : class, IApply, IHaveScenarios, new()
{
    public static async Task Handle(
        TProjection projection,
        IReadOnlyList<IMessageContext> batch,
        CancellationToken cancellationToken
    )
    {
        var configuration = new ProjectionConfiguration<TKey>();

        projection.Configure(configuration);

        var messagesToApply = configuration.Filter(batch);

        if (messagesToApply.Count == 0)
        {
            return;
        }

        foreach (var keyBatch in  messagesToApply.GroupBy(x => configuration.GetKey(x)))
        {
            var firstMessage = keyBatch.First();
            var lastMessage = keyBatch.Last();

            var firstMessageType = firstMessage.MessageType ??
                                   throw new InvalidOperationException(
                                       $"Unable to deserialize message of type {firstMessage.Type}"
                                   );

            var lastMessageType = lastMessage.MessageType ??
                                  throw new InvalidOperationException(
                                      $"Unable to deserialize message of type {lastMessage.Type}"
                                  );

            var firstConfiguration = configuration.GetConfigurationFor(firstMessageType);
            var lastConfiguration = configuration.GetConfigurationFor(lastMessageType);

            var action = firstConfiguration.Action;
            var ignoreWhenNotFound = firstConfiguration.IgnoreWhenNotFound;

            var key = firstConfiguration.GetKey<TKey>(
                firstMessage.Message ??
                throw new InvalidOperationException($"Unable to deserialize message of type {firstMessage.Type}")
            );

            if (lastConfiguration.Action == ProjectionAction.Delete)
            {
                await HandleDelete(projection, key, action, cancellationToken);

                return;
            }

            var result = await LoadState(projection, key, action, ignoreWhenNotFound, cancellationToken);

            var state = keyBatch.ApplyTo(result.State);

            if (result.Loaded)
            {
                await projection.Update(state, cancellationToken);

                return;
            }

            await projection.Create(state, cancellationToken);
        }
    }

    private static async Task<(TState State, bool Loaded)> LoadState(
        TProjection projection,
        TKey key,
        ProjectionAction action,
        bool ignoreWhenNotFound,
        CancellationToken cancellationToken
    )
    {
        TState? state;
        var loaded = false;

        if (action != ProjectionAction.Create)
        {
            state = await projection.Read(key, cancellationToken);

            if (state == null)
            {
                if (!ignoreWhenNotFound && action != ProjectionAction.CreateOrUpdate)
                {
                    throw new InvalidOperationException(
                        $"Cannot {action.ToString().ToLowerInvariant()} {typeof(TState).Name} with key {key} - projection not found"
                    );
                }

                state = new TState();
            }
            else
            {
                loaded = true;
            }
        }
        else
        {
            state = new TState();
        }

        return (state, loaded);
    }

    private static async Task HandleDelete(
        TProjection projection,
        TKey key,
        ProjectionAction initialActionToPerform,
        CancellationToken cancellationToken
    )
    {
        if (initialActionToPerform == ProjectionAction.Create)
        {
            //exit early - nothing to do
            return;
        }

        await projection.Delete(key, cancellationToken);
    }
}
