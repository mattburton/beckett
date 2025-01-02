using TaskHub.TaskLists.Events;

namespace TaskHub.TaskLists.GetList.Tests;

public class GetListHandlerTests
{
    [Fact]
    public async Task returns_ok_when_task_list_exists()
    {
        var id = Guid.NewGuid();
        var name = Guid.NewGuid().ToString();
        var task = Guid.NewGuid().ToString();
        var streamName = TaskHub.TaskLists.TaskList.StreamName(id);
        var messageStore = new FakeMessageStore();
        messageStore.HasExistingMessages(streamName, new TaskListAdded(id, name), new TaskAdded(id, task));

        var result = await GetListHandler.Get(id, messageStore, CancellationToken.None);

        var response = Assert.IsType<Ok<GetListReadModel>>(result);
        Assert.NotNull(response.Value);
        Assert.Equal(id, response.Value.Id);
        Assert.Equal(name, response.Value.Name);
        var taskItem = Assert.Single(response.Value.Tasks);
        Assert.Equal(task, taskItem.Task);
    }

    [Fact]
    public async Task returns_not_found_when_task_list_does_not_exist()
    {
        var id = Guid.NewGuid();
        var messageStore = new FakeMessageStore();

        var result = await GetListHandler.Get(id, messageStore, CancellationToken.None);

        Assert.IsType<NotFound>(result);
    }
}
