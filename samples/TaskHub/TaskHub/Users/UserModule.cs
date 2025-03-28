using Contracts.Users;
using Contracts.Users.Commands;
using Contracts.Users.Queries;

namespace TaskHub.Users;

public class UserModule(ICommandDispatcher commandDispatcher, IQueryDispatcher queryDispatcher) : IUserModule, IModule
{
    public Task Execute(DeleteUserCommand command, CancellationToken cancellationToken) =>
        commandDispatcher.Dispatch(command, cancellationToken);

    public Task<GetUserQuery.Result?> Execute(GetUserQuery query, CancellationToken cancellationToken) =>
        queryDispatcher.Dispatch(query, cancellationToken);

    public Task<GetUsersQuery.Result> Execute(GetUsersQuery query, CancellationToken cancellationToken) =>
        queryDispatcher.Dispatch(query, cancellationToken);

    public Task Execute(RegisterUserCommand command, CancellationToken cancellationToken) =>
        commandDispatcher.Dispatch(command, cancellationToken);
}
