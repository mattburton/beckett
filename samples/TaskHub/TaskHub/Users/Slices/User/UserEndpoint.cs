using TaskHub.Users.Queries;

namespace TaskHub.Users.Slices.User;

public static class UserEndpoint
{
    public static async Task<IResult> Handle(
        string username,
        IQueryBus queryBus,
        CancellationToken cancellationToken
    )
    {
        var result = await queryBus.Send(new UserQuery(username), cancellationToken);

        return result == null ? Results.NotFound() : Results.Ok(result);
    }
}
