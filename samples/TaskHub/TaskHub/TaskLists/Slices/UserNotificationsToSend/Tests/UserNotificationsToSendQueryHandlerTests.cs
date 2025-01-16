using TaskHub.TaskLists.Events;

namespace TaskHub.TaskLists.Slices.UserNotificationsToSend.Tests;

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
            var streamName = TaskListModule.StreamName(taskListId);
            var messageStore = new FakeMessageStore();
            var handler = new UserNotificationsToSendQueryHandler(messageStore);
            var query = new UserNotificationsToSendQuery(taskListId);
            messageStore.HasExistingMessages(streamName, new UserMentionedInTask(taskListId, task, username));

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
        public async Task returns_null()
        {
            var taskListId = Generate.Guid();
            var messageStore = new FakeMessageStore();
            var handler = new UserNotificationsToSendQueryHandler(messageStore);
            var query = new UserNotificationsToSendQuery(taskListId);

            var result = await handler.Handle(query, CancellationToken.None);

            Assert.Null(result);
        }
    }
}
