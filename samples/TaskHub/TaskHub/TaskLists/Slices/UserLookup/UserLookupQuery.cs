namespace TaskHub.TaskLists.Slices.UserLookup;

public record UserLookupQuery(string Username) : IQuery<UserLookupReadModel>;
