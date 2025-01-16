using TaskHub.Users.Slices.GetUser;

namespace TaskHub.Users.Contracts.Queries;

public record GetUserQuery(string Username) : IQuery<GetUserReadModel>;
