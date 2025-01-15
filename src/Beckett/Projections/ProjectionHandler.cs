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

        var firstMessage = messagesToApply[0];
        var lastMessage = messagesToApply[^1];

        var firstMessageType = firstMessage.MessageType ??
                               throw new InvalidOperationException(
                                   $"Unable to deserialize message of type {firstMessage.Type}"
                               );

        var lastMessageType = lastMessage.MessageType ??
                              throw new InvalidOperationException(
                                  $"Unable to deserialize message of type {lastMessage.Type}"
                              );

        var firstMessageConfiguration = configuration.GetConfigurationFor(firstMessageType);
        var lastMessageConfiguration = configuration.GetConfigurationFor(lastMessageType);

        var actionToPerform = firstMessageConfiguration.Action;
        var ignoreIfNotFound = firstMessageConfiguration.IgnoreIfNotFound;

        var key = firstMessageConfiguration.Key(
            firstMessage.Message ??
            throw new InvalidOperationException($"Unable to deserialize message of type {firstMessage.Type}")
        );

        if (lastMessageConfiguration.Action == ProjectionAction.Delete)
        {
            await HandleDelete(projection, key, actionToPerform, cancellationToken);

            return;
        }

        var result = await LoadState(projection, key, actionToPerform, ignoreIfNotFound, cancellationToken);

        var state = messagesToApply.ApplyTo(result.State);

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
