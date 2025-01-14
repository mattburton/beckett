namespace Beckett.Projections;

public static class ProjectionHandler<TProjection, TState, TKey> where TProjection : IProjection<TState, TKey>
    where TState : class, IApply, new()
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

        var startingMessage = messagesToApply[0];
        var lastMessage = messagesToApply[^1];

        var startingMessageType = startingMessage.MessageType ??
                                  throw new InvalidOperationException(
                                      $"Unable to deserialize message of type {startingMessage.Type}"
                                  );

        var lastMessageType = lastMessage.MessageType ??
                              throw new InvalidOperationException(
                                  $"Unable to deserialize message of type {lastMessage.Type}"
                              );

        var startingMessageConfiguration = configuration.GetConfigurationFor(startingMessageType);
        var lastMessageConfiguration = configuration.GetConfigurationFor(lastMessageType);

        var actionToPerform = startingMessageConfiguration.Action;
        var ignoreIfNotFound = startingMessageConfiguration.IgnoreIfNotFound;

        var key = startingMessageConfiguration.Key(
            startingMessage.Message ??
            throw new InvalidOperationException($"Unable to deserialize message of type {startingMessage.Type}")
        );

        if (lastMessageConfiguration.Action == ProjectionAction.Delete)
        {
            await HandleDelete(projection, key, actionToPerform, cancellationToken);

            return;
        }

        var result = await LoadState(projection, key, actionToPerform, ignoreIfNotFound, cancellationToken);

        var state = result.State;

        if (projection is IHandleApply<TState> applyHandler)
        {
            foreach (var message in messagesToApply)
            {
                state = await applyHandler.Apply(state, message, cancellationToken);
            }
        }
        else
        {
            state = messagesToApply.ApplyTo(state);
        }

        if (actionToPerform == ProjectionAction.Create)
        {
            await projection.Create(state, cancellationToken);

            return;
        }

        if (result.Loaded)
        {
            await projection.Update(state, cancellationToken);

            return;
        }

        await projection.Create(state, cancellationToken);
    }

    private static async Task<(TState State, bool Loaded)> LoadState(
        TProjection projection,
        TKey key,
        ProjectionAction actionToPerform,
        bool ignoreIfNotFound,
        CancellationToken cancellationToken
    )
    {
        TState? state;
        var loaded = false;

        if (actionToPerform != ProjectionAction.Create)
        {
            state = await projection.Read(key, cancellationToken);

            if (state == null)
            {
                if (!ignoreIfNotFound)
                {
                    throw new InvalidOperationException(
                        $"Cannot {actionToPerform.ToString().ToLowerInvariant()} {typeof(TState).Name} with key {key} - projection not found"
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

        var state = await projection.Read(key, cancellationToken);

        if (state == null)
        {
            return;
        }

        await projection.Delete(state, cancellationToken);
    }
}
