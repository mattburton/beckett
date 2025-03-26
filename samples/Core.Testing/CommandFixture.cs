using Beckett;
using Core.Commands;

namespace Core.Testing;

public abstract class CommandFixture<T> where T : ICommand
{
    protected CommandSpecification<T> Specification => new();
}

public abstract class CommandFixture<T, TState>
    where T : ICommand<TState> where TState : class, IApply, new()
{
    protected CommandSpecification<T, TState> Specification => new();
}
