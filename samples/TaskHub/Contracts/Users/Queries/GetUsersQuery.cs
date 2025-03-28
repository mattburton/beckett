namespace Contracts.Users.Queries;

public record GetUsersQuery : IQuery<GetUsersQuery.Result>
{
    public record Result(IReadOnlyList<User> Users);

    public record User(string Username, string Email);
}
