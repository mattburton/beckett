namespace TaskLists.NotifyUser;

public record UserNotificationsToSendQuery(Guid TaskListId) : IQuery<UserNotificationsToSendReadModel>
{
    public class Handler(
        IStreamReader reader
    ) : StreamStateQueryHandler<UserNotificationsToSendQuery, UserNotificationsToSendReadModel>(reader)
    {
        protected override IStreamName StreamName(UserNotificationsToSendQuery query) =>
            new TaskListStream(query.TaskListId);
    }
}
