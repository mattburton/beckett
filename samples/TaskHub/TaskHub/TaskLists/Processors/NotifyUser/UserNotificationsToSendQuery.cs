using Core.Streams;
using TaskHub.TaskLists.Streams;

namespace TaskHub.TaskLists.Processors.NotifyUser;

public record UserNotificationsToSendQuery(Guid TaskListId) : IQuery<UserNotificationsToSendReadModel>
{
    public class Handler(
        IStreamReader reader
    ) : IQueryHandler<UserNotificationsToSendQuery, UserNotificationsToSendReadModel>
    {
        public async Task<UserNotificationsToSendReadModel> Handle(
            UserNotificationsToSendQuery query,
            CancellationToken cancellationToken
        )
        {
            var stream = await reader.ReadStream(new TaskListStream(query.TaskListId), cancellationToken);

            return stream.IsEmpty
                ? new UserNotificationsToSendReadModel()
                : stream.ProjectTo<UserNotificationsToSendReadModel>();
        }
    }
}
