namespace Contracts.Users.Queries;

public record GetUsers : IQuery<GetUsers.Result>
{
    public record Result(IReadOnlyList<User> Users);

    public record User(string Username, string Email);
}
