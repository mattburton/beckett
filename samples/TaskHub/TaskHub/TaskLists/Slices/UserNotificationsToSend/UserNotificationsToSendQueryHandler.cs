namespace TaskHub.TaskLists.Slices.UserNotificationsToSend;

public class UserNotificationsToSendQueryHandler(
    IMessageStore messageStore
) : ProjectedStreamQueryHandler<UserNotificationsToSendQuery, UserNotificationsToSendReadModel>(messageStore)
{
    protected override string StreamName(UserNotificationsToSendQuery query) =>
        TaskListModule.StreamName(query.TaskListId);
}
