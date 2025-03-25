using System.Runtime.CompilerServices;
using Beckett;
using Beckett.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Core.Projections;

public static class SubscriptionConfigurationBuilderExtensions
{
    public static ISubscriptionConfigurationBuilder Projection<TProjection, TReadModel, TKey>(
        this ISubscriptionConfigurationBuilder builder,
        ServiceLifetime lifetime = ServiceLifetime.Transient
    ) where TProjection : IProjection<TReadModel, TKey> where TReadModel : class, IApply, new()
    {
        var handlerType = typeof(TProjection);

        builder.Services.TryAdd(new ServiceDescriptor(handlerType, handlerType, lifetime));

        var projection = (IProjection<TReadModel, TKey>)RuntimeHelpers.GetUninitializedObject(typeof(TProjection));

        var configuration = new ProjectionConfiguration<TKey>();

        projection.Configure(configuration);

        configuration.Validate(new TReadModel());

        builder.Messages(configuration.GetMessageTypes());

        builder.Handler(ProjectionHandler<TProjection, TReadModel, TKey>.Handle);

        return builder;
    }
}
