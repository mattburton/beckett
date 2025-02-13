namespace TaskHub.Users.Slices.Users;

public static class UsersEndpoint
{
    public static async Task<IResult> Handle(IQueryBus queryBus, CancellationToken cancellationToken)
    {
        var results = await queryBus.Send(new UsersQuery(), cancellationToken);

        return Results.Ok(results);
    }
}
