using TaskHub.Users.Slices.User;

namespace TaskHub.Users.Contracts.Queries;

public record UserQuery(string Username) : IQuery<UserReadModel>;
