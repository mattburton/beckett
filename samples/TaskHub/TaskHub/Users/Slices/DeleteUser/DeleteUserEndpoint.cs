namespace TaskHub.Users.Slices.DeleteUser;

public static class DeleteUserEndpoint
{
    public static async Task<IResult> Handle(
        string username,
        ICommandExecutor commandExecutor,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await commandExecutor.Execute(
                UserModule.StreamName(username),
                new DeleteUserCommand(username),
                new CommandOptions
                {
                    ExpectedVersion = ExpectedVersion.StreamExists
                },
                cancellationToken
            );

            return Results.Ok();
        }
        catch (StreamDoesNotExistException)
        {
            return Results.Conflict();
        }
    }
}
