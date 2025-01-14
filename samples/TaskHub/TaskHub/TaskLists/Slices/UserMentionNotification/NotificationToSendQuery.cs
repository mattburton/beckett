namespace TaskHub.TaskLists.Slices.UserMentionNotification;

public record NotificationToSendQuery(Guid TaskListId) : IQuery<NotificationToSendReadModel>
{
    public class Handler(
        IMessageStore messageStore
    ) : ProjectedStreamQueryHandler<NotificationToSendQuery, NotificationToSendReadModel>(messageStore)
    {
        protected override string StreamName(NotificationToSendQuery query) =>
            TaskListModule.StreamName(query.TaskListId);
    }
}
