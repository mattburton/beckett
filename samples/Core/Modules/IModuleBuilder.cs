using Beckett;
using Core.Contracts;
using Core.Processors;
using Core.Projections;

namespace Core.Modules;

public interface IModuleBuilder
{
    void AddProcessor<TProcessor>(string category, string? name = null) where TProcessor : class, IProcessor;

    void AddProcessor<TProcessor, TMessage>(string? name = null) where TProcessor : class, IProcessor<TMessage>
        where TMessage : class, ISupportSubscriptions;

    void AddProjection<TProjection, TReadModel, TKey>(string? name = null)
        where TProjection : IProjection<TReadModel, TKey> where TReadModel : class, IApply, new();
}

public class ModuleBuilder(IBeckettBuilder builder) : IModuleBuilder
{
    public void AddProcessor<TProcessor>(string category, string? name = null) where TProcessor : class, IProcessor
    {
        builder.AddSubscription(name ?? typeof(TProcessor).Name)
            .Category(category)
            .Handler(ProcessorHandler.For(typeof(TProcessor)))
            .StartingPosition(StartingPosition.Latest);
    }

    public void AddProcessor<TProcessor, TMessage>(string? name = null) where TProcessor : class, IProcessor<TMessage>
        where TMessage : class, ISupportSubscriptions
    {
        builder.AddSubscription(name ?? typeof(TProcessor).Name)
            .Message<TMessage>()
            .Handler(ProcessorHandler.For(typeof(TProcessor)))
            .StartingPosition(StartingPosition.Latest);
    }

    public void AddProjection<TProjection, TReadModel, TKey>(string? name = null)
        where TProjection : IProjection<TReadModel, TKey> where TReadModel : class, IApply, new()
    {
        builder.AddSubscription(name ?? typeof(TProjection).Name)
            .Projection<TProjection, TReadModel, TKey>()
            .StartingPosition(StartingPosition.Earliest);
    }
}
