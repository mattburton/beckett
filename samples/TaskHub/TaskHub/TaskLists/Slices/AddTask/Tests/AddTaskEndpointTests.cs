namespace TaskHub.TaskLists.Slices.AddTask.Tests;

public class AddTaskEndpointTests
{
    [Fact]
    public async Task executes_command()
    {
        var taskListId = Generate.Guid();
        var name = Generate.String();
        var expectedStreamName = TaskListModule.StreamName(taskListId);
        var expectedCommand = new AddTaskCommand(taskListId, name);
        var commandExecutor = new FakeCommandExecutor();
        var request = new AddTaskEndpoint.Request(name);

        await AddTaskEndpoint.Handle(taskListId, request, commandExecutor, CancellationToken.None);

        Assert.NotNull(commandExecutor.Received);
        Assert.Equal(expectedStreamName, commandExecutor.Received.StreamName);
        Assert.Equal(expectedCommand, commandExecutor.Received.Command);
    }

    [Fact]
    public async Task returns_ok_with_result_when_successful()
    {
        var taskListId = Generate.Guid();
        var name = Generate.String();
        var commandExecutor = new FakeCommandExecutor();
        var request = new AddTaskEndpoint.Request(name);

        var result = await AddTaskEndpoint.Handle(taskListId, request, commandExecutor, CancellationToken.None);

        var response = Assert.IsType<Ok<AddTaskEndpoint.Response>>(result);
        Assert.NotNull(response.Value);
        Assert.Equal(taskListId, response.Value.TaskListId);
        Assert.Equal(name, response.Value.Task);
    }

    [Fact]
    public async Task returns_conflict_when_task_already_added()
    {
        var taskListId = Generate.Guid();
        var name = Generate.String();
        var commandExecutor = new FakeCommandExecutor();
        var request = new AddTaskEndpoint.Request(name);
        commandExecutor.Throws(new TaskAlreadyAddedException());

        var result = await AddTaskEndpoint.Handle(taskListId, request, commandExecutor, CancellationToken.None);

        Assert.IsType<Conflict>(result);
    }
}
