using Beckett;
using Contracts.Users.Commands;

namespace API.V1.Users;

public static class RegisterUserEndpoint
{
    public static async Task<IResult> Handle(
        Request request,
        IUserModule module,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await module.Execute(new RegisterUserCommand(request.Username, request.Email), cancellationToken);

            return Results.Ok();
        }
        catch (StreamAlreadyExistsException)
        {
            return Results.Conflict();
        }
    }

    public record Request(string Username, string Email);
}
