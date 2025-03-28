using Beckett;
using Core.Commands;
using Core.Contracts;

namespace Core.Testing;

public abstract class CommandHandlerFixture<TCommand, TCommandHandler> where TCommand : ICommand
    where TCommandHandler : ICommandHandler<TCommand>, new()
{
    protected CommandHandlerSpecification<TCommand, TCommandHandler> Specification => new();
}

public abstract class CommandHandlerFixture<TCommand, TCommandHandler, TState>
    where TCommand : ICommand
    where TCommandHandler : ICommandHandler<TCommand, TState>, new()
    where TState : class, IApply, new()
{
    protected CommandHandlerSpecification<TCommand, TCommandHandler, TState> Specification => new();
}
