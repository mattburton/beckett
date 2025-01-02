using Microsoft.AspNetCore.Http.HttpResults;

namespace TaskHub.TaskLists.AddTask.Tests;

public class AddTaskHandlerTests
{
    [Fact]
    public async Task executes_add_task_command()
    {
        var taskListId = Guid.NewGuid();
        var name = Guid.NewGuid().ToString();
        var expectedStreamName = TaskHub.TaskLists.TaskList.StreamName(taskListId);
        var expectedCommand = new AddTaskCommand(taskListId, name);
        var commandExecutor = new FakeCommandExecutor();
        var request = new AddTaskHandler.Request(name);

        await AddTaskHandler.Post(taskListId, request, commandExecutor, CancellationToken.None);

        Assert.True(commandExecutor.Executed);
        Assert.Equal(expectedStreamName, commandExecutor.ReceivedStreamName);
        Assert.Equal(expectedCommand, commandExecutor.ReceivedCommand);
    }

    [Fact]
    public async Task returns_ok_when_successful()
    {
        var taskListId = Guid.NewGuid();
        var name = Guid.NewGuid().ToString();
        var commandExecutor = new FakeCommandExecutor();
        var request = new AddTaskHandler.Request(name);

        var result = await AddTaskHandler.Post(taskListId, request, commandExecutor, CancellationToken.None);

        var response = Assert.IsType<Ok<AddTaskHandler.Response>>(result);
        Assert.NotNull(response.Value);
        Assert.Equal(taskListId, response.Value.TaskListId);
        Assert.Equal(name, response.Value.Task);
    }

    [Fact]
    public async Task returns_conflict_when_task_already_added()
    {
        var taskListId = Guid.NewGuid();
        var name = Guid.NewGuid().ToString();
        var commandExecutor = new FakeCommandExecutor();
        var request = new AddTaskHandler.Request(name);
        commandExecutor.Throws(new TaskAlreadyAddedException());

        var result = await AddTaskHandler.Post(taskListId, request, commandExecutor, CancellationToken.None);

        Assert.IsType<Conflict>(result);
    }
}
