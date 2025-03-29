using Beckett;
using Contracts.Users.Commands;

namespace API.V1.Users;

public static class DeleteUserEndpoint
{
    public static async Task<IResult> Handle(
        string username,
        IUserModule module,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await module.Execute(new DeleteUser(username), cancellationToken);

            return Results.Ok();
        }
        catch (StreamDoesNotExistException)
        {
            return Results.Conflict();
        }
    }
}
