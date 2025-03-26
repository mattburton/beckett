using Beckett;
using Core.Commands;
using TaskHub.Users.Slices.DeleteUser;

namespace API.V1.Users;

public static class DeleteUserEndpoint
{
    public static async Task<IResult> Handle(
        string username,
        ICommandBus commandBus,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await commandBus.Send(new DeleteUserCommand(username), cancellationToken);

            return Results.Ok();
        }
        catch (StreamDoesNotExistException)
        {
            return Results.Conflict();
        }
    }
}
