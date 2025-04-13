using System.Runtime.CompilerServices;
using Beckett.Configuration;
using Core.State;

namespace Core.Projections;

public static class SubscriptionConfigurationBuilderExtensions
{
    public static ISubscriptionConfigurationBuilder Projection<TProjection, TState>(
        this ISubscriptionConfigurationBuilder builder
    ) where TProjection : IProjection<TState> where TState : class, IStateView, new()
    {
        var projection = (IProjection<TState>)RuntimeHelpers.GetUninitializedObject(typeof(TProjection));

        var configuration = new ProjectionConfiguration();

        projection.Configure(configuration);

        configuration.Validate(new TState());

        builder.Messages(configuration.GetMessageTypes());

        builder.Handler(ProjectionHandler<TState>.Handle);

        return builder;
    }
}
