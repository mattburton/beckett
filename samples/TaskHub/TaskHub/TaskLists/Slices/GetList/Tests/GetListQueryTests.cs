using TaskHub.TaskLists.Events;

namespace TaskHub.TaskLists.Slices.GetList.Tests;

public class GetListQueryTests
{
    public class when_list_exists
    {
        [Fact]
        public async Task returns_read_model()
        {
            var id = Generate.Guid();
            var name = Generate.String();
            var task = Generate.String();
            var streamName = TaskListModule.StreamName(id);
            var messageStore = new FakeMessageStore();
            var handler = new GetListQuery.Handler(messageStore);
            var query = new GetListQuery(id);
            messageStore.HasExistingMessages(streamName, new TaskListAdded(id, name), new TaskAdded(id, task));

            var result = await handler.Handle(query, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(id, result.Id);
            Assert.Equal(name, result.Name);
            var taskItem = Assert.Single(result.Tasks);
            Assert.Equal(task, taskItem.Task);
        }
    }

    public class when_list_does_not_exist
    {
        [Fact]
        public async Task returns_null()
        {
            var id = Generate.Guid();
            var messageStore = new FakeMessageStore();
            var handler = new GetListQuery.Handler(messageStore);
            var query = new GetListQuery(id);

            var result = await handler.Handle(query, CancellationToken.None);

            Assert.Null(result);
        }
    }
}
