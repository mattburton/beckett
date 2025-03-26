using Beckett;
using Core.Commands;
using TaskHub.Users.Slices.RegisterUser;

namespace API.V1.Users;

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
