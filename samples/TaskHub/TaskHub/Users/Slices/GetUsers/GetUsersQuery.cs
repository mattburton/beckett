namespace TaskHub.Users.Slices.GetUsers;

public record GetUsersQuery : IQuery<IReadOnlyList<TaskLists.Slices.UserLookup.UserLookupReadModel>>;
