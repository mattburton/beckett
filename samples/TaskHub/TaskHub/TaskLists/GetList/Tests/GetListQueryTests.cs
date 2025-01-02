using TaskHub.TaskLists.Events;

namespace TaskHub.TaskLists.GetList.Tests;

public class GetListQueryTests
{
    [Fact]
    public async Task returns_get_task_list_read_model()
    {
        var id = Guid.NewGuid();
        var name = Guid.NewGuid().ToString();
        var task = Guid.NewGuid().ToString();
        var messageStore = new FakeMessageStore();
        messageStore.HasExistingMessages(TaskHub.TaskLists.TaskList.StreamName(id), new TaskListAdded(id, name), new TaskAdded(id, task));

        var result = await new GetListQuery(id).Execute(messageStore, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
        Assert.Equal(name, result.Name);
        var taskItem = Assert.Single(result.Tasks);
        Assert.Equal(task, taskItem.Task);
    }
}
