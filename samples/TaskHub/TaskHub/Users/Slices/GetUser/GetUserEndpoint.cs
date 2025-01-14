namespace TaskHub.Users.Slices.GetUser;

public static class GetUserEndpoint
{
    public static async Task<IResult> Handle(
        string username,
        IQueryExecutor queryExecutor,
        CancellationToken cancellationToken
    )
    {
        var result = await queryExecutor.Execute(new GetUserQuery(username), cancellationToken);

        return result == null ? Results.NotFound() : Results.Ok(result);
    }
}
