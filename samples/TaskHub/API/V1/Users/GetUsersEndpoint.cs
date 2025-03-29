using Contracts.Users.Queries;

namespace API.V1.Users;

public static class GetUsersEndpoint
{
    public static async Task<IResult> Handle(IUserModule module, CancellationToken cancellationToken)
    {
        var results = await module.Execute(new GetUsers(), cancellationToken);

        return Results.Ok(results);
    }
}
