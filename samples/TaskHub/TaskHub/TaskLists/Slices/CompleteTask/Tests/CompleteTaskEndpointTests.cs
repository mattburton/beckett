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
        var commandBus = new FakeCommandBus();

        await CompleteTaskEndpoint.Handle(taskListId, task, commandBus, CancellationToken.None);

        var actualCommand = Assert.IsType<CompleteTaskCommand>(commandBus.Received);
        Assert.Equal(expectedStreamName, actualCommand.StreamName());
        Assert.Equal(expectedCommand, actualCommand);
    }

    [Fact]
    public async Task returns_ok_with_result_when_successful()
    {
        var taskListId = Generate.Guid();
        var task = Generate.String();
        var commandBus = new FakeCommandBus();

        var result = await CompleteTaskEndpoint.Handle(taskListId, task, commandBus, CancellationToken.None);

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
        var commandBus = new FakeCommandBus();
        commandBus.Throws(new TaskAlreadyCompletedException());

        var result = await CompleteTaskEndpoint.Handle(taskListId, task, commandBus, CancellationToken.None);

        Assert.IsType<Conflict>(result);
    }
}
