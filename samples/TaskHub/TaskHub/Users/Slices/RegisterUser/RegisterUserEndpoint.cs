namespace TaskHub.Users.Slices.RegisterUser;

public static class RegisterUserEndpoint
{
    public static async Task<IResult> Handle(
        Request request,
        ICommandExecutor commandExecutor,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await commandExecutor.Execute(
                UserModule.StreamName(request.Username),
                new RegisterUserCommand(request.Username, request.Email),
                new CommandOptions
                {
                    ExpectedVersion = ExpectedVersion.StreamDoesNotExist
                },
                cancellationToken
            );

            return Results.Ok();
        }
        catch (StreamAlreadyExistsException)
        {
            return Results.Conflict();
        }
    }

    public record Request(string Username, string Email);
}
