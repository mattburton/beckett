using System.Runtime.CompilerServices;
using Beckett;
using Beckett.Configuration;
using Core.MessageHandling;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Core.Projections;

public static class SubscriptionConfigurationBuilderExtensions
{
    public static ISubscriptionConfigurationBuilder Projection<TProjection, TState, TKey>(
        this ISubscriptionConfigurationBuilder builder,
        ServiceLifetime lifetime = ServiceLifetime.Transient
    ) where TProjection : IProjection<TState, TKey> where TState : class, IApply, IHaveScenarios, new()
    {
        var handlerType = typeof(TProjection);

        builder.Services.TryAdd(new ServiceDescriptor(handlerType, handlerType, lifetime));

        var projection = (IProjection<TState, TKey>)RuntimeHelpers.GetUninitializedObject(typeof(TProjection));

        var configuration = new ProjectionConfiguration<TKey>();

        projection.Configure(configuration);

        configuration.Validate(new TState());

        builder.Messages(configuration.GetMessageTypes());

        builder.Handler(ProjectionHandler<TProjection, TState, TKey>.Handle);

        return builder;
    }
}
