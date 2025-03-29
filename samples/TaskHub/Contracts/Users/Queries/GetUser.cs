namespace Contracts.Users.Queries;

public record GetUser(string Username) : IQuery<GetUser.Result?>
{
    public record Result(string Username, string Email);
}
