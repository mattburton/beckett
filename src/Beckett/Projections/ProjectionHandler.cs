namespace Beckett.Projections;

public static class ProjectionHandler<TProjection, TState, TKey> where TProjection : IProjection<TState, TKey>
    where TState : IApply, new()
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

        var result = await LoadModel(projection, key, actionToPerform, ignoreIfNotFound, cancellationToken);

        var model = result.Model;

        if (projection is IHandleApply<TState> applyHandler)
        {
            foreach (var message in messagesToApply)
            {
                model = await applyHandler.Apply(model, message.Message!, cancellationToken);
            }
        }
        else
        {
            model = messagesToApply.Select(x => x.Message!).ApplyTo(model);
        }

        if (actionToPerform == ProjectionAction.Create)
        {
            await projection.Create(model, cancellationToken);

            return;
        }

        if (result.Loaded)
        {
            await projection.Update(model, cancellationToken);

            return;
        }

        await projection.Create(model, cancellationToken);
    }

    private static async Task<(TState Model, bool Loaded)> LoadModel(
        TProjection projection,
        TKey key,
        ProjectionAction actionToPerform,
        bool ignoreIfNotFound,
        CancellationToken cancellationToken
    )
    {
        TState? model;
        var loaded = false;

        if (actionToPerform != ProjectionAction.Create)
        {
            model = await projection.Load(key, cancellationToken);

            if (model == null)
            {
                if (!ignoreIfNotFound)
                {
                    throw new InvalidOperationException(
                        $"Cannot {actionToPerform.ToString().ToLowerInvariant()} {typeof(TState).Name} with key {key} - read model not found"
                    );
                }

                model = new TState();
            }
            else
            {
                loaded = true;
            }
        }
        else
        {
            model = new TState();
        }

        return (model, loaded);
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

        var model = await projection.Load(key, cancellationToken);

        if (model == null)
        {
            return;
        }

        await projection.Delete(model, cancellationToken);
    }
}
