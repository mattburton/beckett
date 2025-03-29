using Contracts.Users.Queries;

namespace API.V1.Users;

public static class GetUserEndpoint
{
    public static async Task<IResult> Handle(
        string username,
        IUserModule module,
        CancellationToken cancellationToken
    )
    {
        var result = await module.Execute(new GetUser(username), cancellationToken);

        return result == null ? Results.NotFound() : Results.Ok(result);
    }
}
