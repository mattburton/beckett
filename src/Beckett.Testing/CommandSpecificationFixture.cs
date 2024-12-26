using Beckett.Commands;

namespace Beckett.Testing;

public abstract class CommandSpecificationFixture<T> where T : ICommand
{
    protected CommandSpecification<T> Specification => new();
}

public abstract class CommandSpecificationFixture<T, TState> where T : ICommand<TState> where TState : IApply, new()
{
    protected CommandSpecification<T, TState> Specification => new();
}
