namespace TaskHub.TaskLists.Slices.UserNotificationsToSend;

public record UserNotificationsToSendQuery(Guid TaskListId) : IQuery<UserNotificationsToSendReadModel>;
