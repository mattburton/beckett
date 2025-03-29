using Contracts.Users.Commands;
using Contracts.Users.Exceptions;
using Contracts.Users.Queries;

namespace Contracts.Users;

public interface IUserModule : IModule
{
    /// <summary>
    /// Delete user
    /// </summary>
    /// <param name="command"></param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="ResourceNotFoundException"></exception>
    /// <exception cref="UserAlreadyDeletedException">User has already been deleted</exception>
    Task Execute(DeleteUser command, CancellationToken cancellationToken);

    /// <summary>
    /// Get user by username
    /// </summary>
    /// <param name="query"></param>
    /// <param name="cancellationToken"></param>
    Task<GetUser.Result?> Execute(GetUser query, CancellationToken cancellationToken);

    /// <summary>
    /// Get users
    /// </summary>
    /// <param name="query"></param>
    /// <param name="cancellationToken"></param>
    Task<GetUsers.Result> Execute(GetUsers query, CancellationToken cancellationToken);

    /// <summary>
    /// Register user
    /// </summary>
    /// <param name="command"></param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="ResourceAlreadyExistsException">Another user with the same username has already been added</exception>
    Task Execute(RegisterUser command, CancellationToken cancellationToken);
}
