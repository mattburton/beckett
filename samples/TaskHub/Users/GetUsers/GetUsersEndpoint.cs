namespace Users.GetUsers;

public static class GetUsersEndpoint
{
    public static async Task<IResult> Handle(IUserModule module, CancellationToken cancellationToken)
    {
        var results = await module.Execute(new GetUsersQuery(), cancellationToken);

        return Results.Ok(Response.From(results));
    }

    public record Response(List<Response.User> Users)
    {
        public static Response From(IReadOnlyList<GetUsersReadModel>? results)
        {
            return new Response(results?.Select(x => new Response.User(x.Username, x.Email)).ToList() ?? []);
        }

        public record User(string Username, string Email);
    }
}
