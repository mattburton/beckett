namespace Contracts.Users.Queries;

public record GetUserQuery(string Username) : IQuery<GetUserQuery.Result?>
{
    public record Result(string Username, string Email);
}
