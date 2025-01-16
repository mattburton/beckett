using TaskHub.Users.Contracts.Queries;

namespace TaskHub.Users.Slices.GetUser;

public static class GetUserEndpoint
{
    public static async Task<IResult> Handle(
        string username,
        IQueryDispatcher queryDispatcher,
        CancellationToken cancellationToken
    )
    {
        var result = await queryDispatcher.Dispatch(new GetUserQuery(username), cancellationToken);

        return result == null ? Results.NotFound() : Results.Ok(result);
    }
}
