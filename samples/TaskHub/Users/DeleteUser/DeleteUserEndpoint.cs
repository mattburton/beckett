namespace Users.DeleteUser;

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
            await module.Execute(new DeleteUserCommand(username), cancellationToken);

            return Results.Ok();
        }
        catch (StreamDoesNotExistException)
        {
            return Results.Conflict();
        }
    }
}
