namespace TaskHub.Users.Slices.GetUsers;

public static class GetUsersEndpoint
{
    public static async Task<IResult> Handle(IQueryDispatcher queryDispatcher, CancellationToken cancellationToken)
    {
        var results = await queryDispatcher.Dispatch(new GetUsersQuery(), cancellationToken);

        return Results.Ok(results);
    }
}
