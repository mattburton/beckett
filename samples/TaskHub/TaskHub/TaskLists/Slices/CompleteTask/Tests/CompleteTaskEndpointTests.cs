namespace TaskHub.TaskLists.Slices.CompleteTask.Tests;

public class CompleteTaskEndpointTests
{
    [Fact]
    public async Task executes_command()
    {
        var taskListId = Generate.Guid();
        var task = Generate.String();
        var expectedStreamName = TaskListModule.StreamName(taskListId);
        var expectedCommand = new CompleteTaskCommand(taskListId, task);
        var commandExecutor = new FakeCommandExecutor();

        await CompleteTaskEndpoint.Handle(taskListId, task, commandExecutor, CancellationToken.None);

        Assert.NotNull(commandExecutor.Received);
        Assert.Equal(expectedStreamName, commandExecutor.Received.StreamName);
        Assert.Equal(expectedCommand, commandExecutor.Received.Command);
    }

    [Fact]
    public async Task returns_ok_with_result_when_successful()
    {
        var taskListId = Generate.Guid();
        var task = Generate.String();
        var commandExecutor = new FakeCommandExecutor();

        var result = await CompleteTaskEndpoint.Handle(taskListId, task, commandExecutor, CancellationToken.None);

        var response = Assert.IsType<Ok<CompleteTaskEndpoint.Response>>(result);
        Assert.NotNull(response.Value);
        Assert.Equal(taskListId, response.Value.TaskListId);
        Assert.Equal(task, response.Value.Task);
    }

    [Fact]
    public async Task returns_conflict_when_task_already_added()
    {
        var taskListId = Generate.Guid();
        var task = Generate.String();
        var commandExecutor = new FakeCommandExecutor();
        commandExecutor.Throws(new TaskAlreadyCompletedException());

        var result = await CompleteTaskEndpoint.Handle(taskListId, task, commandExecutor, CancellationToken.None);

        Assert.IsType<Conflict>(result);
    }
}
