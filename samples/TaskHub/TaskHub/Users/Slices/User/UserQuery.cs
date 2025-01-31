using TaskHub.Users.Slices.User;

namespace TaskHub.Users.Queries;

public record UserQuery(string Username) : IQuery<UserReadModel>;
