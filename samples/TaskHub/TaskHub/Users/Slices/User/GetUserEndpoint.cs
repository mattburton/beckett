using TaskHub.Users.Contracts.Queries;

namespace TaskHub.Users.Slices.User;

public static class GetUserEndpoint
{
    public static async Task<IResult> Handle(
        string username,
        IQueryDispatcher queryDispatcher,
        CancellationToken cancellationToken
    )
    {
        var result = await queryDispatcher.Dispatch(new UserQuery(username), cancellationToken);

        return result == null ? Results.NotFound() : Results.Ok(result);
    }
}
