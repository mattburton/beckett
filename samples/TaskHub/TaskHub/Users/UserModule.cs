using Contracts.Users;
using Contracts.Users.Commands;
using Contracts.Users.Queries;

namespace TaskHub.Users;

public class UserModule(IDispatcher dispatcher) : IUserModule
{
    public Task Execute(DeleteUser command, CancellationToken cancellationToken) =>
        dispatcher.Dispatch(command, cancellationToken);

    public Task<GetUser.Result?> Execute(GetUser query, CancellationToken cancellationToken) =>
        dispatcher.Dispatch(query, cancellationToken);

    public Task<GetUsers.Result> Execute(GetUsers query, CancellationToken cancellationToken) =>
        dispatcher.Dispatch(query, cancellationToken);

    public Task Execute(RegisterUser command, CancellationToken cancellationToken) =>
        dispatcher.Dispatch(command, cancellationToken);
}
