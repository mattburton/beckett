namespace TaskHub.Users.Slices.RegisterUser;

public static class RegisterUserEndpoint
{
    public static async Task<IResult> Handle(
        Request request,
        ICommandBus commandBus,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await commandBus.Send(new RegisterUserCommand(request.Username, request.Email), cancellationToken);

            return Results.Ok();
        }
        catch (StreamAlreadyExistsException)
        {
            return Results.Conflict();
        }
    }

    public record Request(string Username, string Email);
}
