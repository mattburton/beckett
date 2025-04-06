namespace Users.GetUser;

public static class GetUserEndpoint
{
    public static async Task<IResult> Handle(
        string username,
        IUserModule module,
        CancellationToken cancellationToken
    )
    {
        var result = await module.Execute(new GetUserQuery(username), cancellationToken);

        return result == null ? Results.NotFound() : Results.Ok(Response.From(result));
    }

    public record Response(string Username, string Email)
    {
        public static Response From(GetUserReadModel result) => new(result.Username, result.Email);
    }
}
