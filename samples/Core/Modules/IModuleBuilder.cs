using Beckett;
using Core.Contracts;
using Core.Processors;
using Core.Projections;
using Core.Scenarios;

namespace Core.Modules;

public interface IModuleBuilder
{
    void AddProcessor<TProcessor>(string category, string name) where TProcessor : class, IProcessor;

    void AddProcessor<TProcessor, TMessage>(string name) where TProcessor : class, IProcessor<TMessage>
        where TMessage : class, IProcessorInput;

    void AddProjection<TProjection, TState, TKey>(string name)
        where TProjection : IProjection<TState, TKey> where TState : class, IApply, IHaveScenarios, new();
}

public class ModuleBuilder(IModuleConfiguration configuration, IBeckettBuilder builder) : IModuleBuilder
{
    public void AddProcessor<TProcessor>(string category, string name) where TProcessor : class, IProcessor
    {
        builder.AddSubscription($"{configuration.ModuleName}:{name}")
            .Category(category)
            .Handler(ProcessorHandler.For(typeof(TProcessor)))
            .StartingPosition(StartingPosition.Latest);
    }

    public void AddProcessor<TProcessor, TMessage>(string name) where TProcessor : class, IProcessor<TMessage>
        where TMessage : class, IProcessorInput
    {
        builder.AddSubscription($"{configuration.ModuleName}:{name}")
            .Message<TMessage>()
            .Handler(ProcessorHandler.For(typeof(TProcessor)))
            .StartingPosition(StartingPosition.Latest);
    }

    public void AddProjection<TProjection, TState, TKey>(string name)
        where TProjection : IProjection<TState, TKey> where TState : class, IApply, IHaveScenarios, new()
    {
        builder.AddSubscription($"{configuration.ModuleName}:{name}")
            .Projection<TProjection, TState, TKey>()
            .StartingPosition(StartingPosition.Earliest);
    }
}
