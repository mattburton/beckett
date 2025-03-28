using Core.Streams;
using TaskHub.TaskLists.Events;
using TaskHub.TaskLists.Processors.NotifyUser;
using TaskHub.TaskLists.Streams;

namespace Tests.TaskLists.Processors.NotifyUser;

public class UserNotificationsToSendQueryHandlerTests
{
    public class when_list_exists
    {
        [Fact]
        public async Task returns_read_model()
        {
            var taskListId = Generate.Guid();
            var task = Generate.String();
            var username = Generate.String();
            var streamName = new TaskListStream(taskListId);
            var reader = new FakeStreamReader();
            var handler = new UserNotificationsToSendQuery.Handler(reader);
            var query = new UserNotificationsToSendQuery(taskListId);
            reader.HasExistingStream(streamName, new UserMentionedInTask(taskListId, task, username));

            var result = await handler.Handle(query, CancellationToken.None);

            Assert.NotNull(result);
            var notification = Assert.Single(result.Notifications);
            Assert.Equal(task, notification.Key);
            Assert.False(notification.Value);
        }
    }

    public class when_list_does_not_exist
    {
        [Fact]
        public async Task returns_empty_result()
        {
            var taskListId = Generate.Guid();
            var reader = new FakeStreamReader();
            var handler = new UserNotificationsToSendQuery.Handler(reader);
            var query = new UserNotificationsToSendQuery(taskListId);

            var result = await handler.Handle(query, CancellationToken.None);

            Assert.Empty(result.Notifications);
        }
    }
}
