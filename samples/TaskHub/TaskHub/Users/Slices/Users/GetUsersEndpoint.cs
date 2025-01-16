namespace TaskHub.Users.Slices.Users;

public static class GetUsersEndpoint
{
    public static async Task<IResult> Handle(IQueryDispatcher queryDispatcher, CancellationToken cancellationToken)
    {
        var results = await queryDispatcher.Dispatch(new UsersQuery(), cancellationToken);

        return Results.Ok(results);
    }
}
