using Beckett;

namespace Core.Projections;

public static class ProjectionHandler<TProjection, TReadModel, TKey> where TProjection : IProjection<TReadModel, TKey>
    where TReadModel : class, IApply, new()
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

            var readModel = keyBatch.ApplyTo(result.ReadModel);

            if (result.Loaded)
            {
                await projection.Update(readModel, cancellationToken);

                return;
            }

            await projection.Create(readModel, cancellationToken);
        }
    }

    private static async Task<(TReadModel ReadModel, bool Loaded)> LoadState(
        TProjection projection,
        TKey key,
        ProjectionAction action,
        bool ignoreWhenNotFound,
        CancellationToken cancellationToken
    )
    {
        TReadModel? readModel;
        var loaded = false;

        if (action != ProjectionAction.Create)
        {
            readModel = await projection.Read(key, cancellationToken);

            if (readModel == null)
            {
                if (!ignoreWhenNotFound && action != ProjectionAction.CreateOrUpdate)
                {
                    throw new InvalidOperationException(
                        $"Cannot {action.ToString().ToLowerInvariant()} {typeof(TReadModel).Name} with key {key} - projection not found"
                    );
                }

                readModel = new TReadModel();
            }
            else
            {
                loaded = true;
            }
        }
        else
        {
            readModel = new TReadModel();
        }

        return (readModel, loaded);
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
