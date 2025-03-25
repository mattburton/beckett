namespace Tests.Infrastructure.Tests;

public abstract class CommandSpecificationFixture<T> where T : ICommand
{
    protected CommandSpecification<T> Specification => new();
}

public abstract class CommandSpecificationFixture<T, TState>
    where T : ICommand<TState> where TState : class, IApply, new()
{
    protected CommandSpecification<T, TState> Specification => new();
}
