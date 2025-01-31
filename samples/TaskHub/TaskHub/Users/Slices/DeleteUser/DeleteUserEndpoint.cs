namespace TaskHub.Users.Slices.DeleteUser;

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
