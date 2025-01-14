namespace TaskHub.Users.Slices.GetUsers;

public static class GetUsersEndpoint
{
    public static async Task<IResult> Handle(IQueryExecutor queryExecutor, CancellationToken cancellationToken)
    {
        var results = await queryExecutor.Execute(new GetUsersQuery(), cancellationToken);

        return Results.Ok(results);
    }
}
