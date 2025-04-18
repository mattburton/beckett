using Beckett;
using Beckett.Configuration;
using Core.Contracts;
using Core.Processors;
using Core.Projections;
using Core.State;

namespace Core.Modules;

public interface IModuleBuilder
{
    ISubscriptionConfigurationBuilder AddProcessor<TProcessor>(string category, string? name = null)
        where TProcessor : class, IProcessor;

    ISubscriptionConfigurationBuilder AddProcessor<TProcessor, TMessage>(string? name = null)
        where TProcessor : class, IProcessor<TMessage>
        where TMessage : class, IProcessorInput;

    ISubscriptionConfigurationBuilder AddBatchProcessor<TBatchProcessor>(string? name = null)
        where TBatchProcessor : class, IBatchProcessor;

    ISubscriptionConfigurationBuilder AddProjection<TProjection, TState>(string? name = null)
        where TProjection : IProjection<TState> where TState : class, IStateView, new();
}

public class ModuleBuilder(IModuleConfiguration configuration, IBeckettBuilder builder) : IModuleBuilder
{
    public ISubscriptionConfigurationBuilder AddProcessor<TProcessor>(string category, string? name = null)
        where TProcessor : class, IProcessor
    {
        return builder.AddSubscription($"{configuration.ModuleName}:{name ?? typeof(TProcessor).Name}")
            .Category(category)
            .Handler(ProcessorHandler.For(typeof(TProcessor)))
            .StartingPosition(StartingPosition.Latest);
    }

    public ISubscriptionConfigurationBuilder AddProcessor<TProcessor, TMessage>(string? name = null)
        where TProcessor : class, IProcessor<TMessage>
        where TMessage : class, IProcessorInput
    {
        return builder.AddSubscription($"{configuration.ModuleName}:{name ?? typeof(TProcessor).Name}")
            .Message<TMessage>()
            .Handler(ProcessorHandler.For(typeof(TProcessor)))
            .StartingPosition(StartingPosition.Latest);
    }

    public ISubscriptionConfigurationBuilder AddBatchProcessor<TBatchProcessor>(string? name = null)
        where TBatchProcessor : class, IBatchProcessor
    {
        return builder.AddSubscription($"{configuration.ModuleName}:{name ?? typeof(TBatchProcessor).Name}")
            .Handler(BatchProcessorHandler.For(typeof(TBatchProcessor)))
            .StartingPosition(StartingPosition.Latest);
    }

    public ISubscriptionConfigurationBuilder AddProjection<TProjection, TState>(string? name = null)
        where TProjection : IProjection<TState> where TState : class, IStateView, new()
    {
        return builder.AddSubscription($"{configuration.ModuleName}:{name ?? typeof(TProjection).Name}")
            .Projection<TProjection, TState>()
            .StartingPosition(StartingPosition.Earliest);
    }
}
