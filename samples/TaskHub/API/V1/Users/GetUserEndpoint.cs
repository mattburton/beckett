using Core.Queries;
using TaskHub.Users.Queries;

namespace API.V1.Users;

public static class GetUserEndpoint
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
