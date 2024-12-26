using TaskHub.TaskList.CompleteTask;

namespace Tests.TaskList.CompleteTask;

public class CompleteTaskHandlerTests
{
    [Fact]
    public async Task executes_complete_task_command()
    {
        var taskListId = Guid.NewGuid();
        var task = Guid.NewGuid().ToString();
        var expectedStreamName = TaskHub.TaskList.TaskList.StreamName(taskListId);
        var expectedCommand = new CompleteTaskCommand(taskListId, task);
        var commandExecutor = new FakeCommandExecutor();

        await CompleteTaskHandler.Post(taskListId, task, commandExecutor, CancellationToken.None);

        Assert.True(commandExecutor.Executed);
        Assert.Equal(expectedStreamName, commandExecutor.ReceivedStreamName);
        Assert.Equal(expectedCommand, commandExecutor.ReceivedCommand);
    }

    [Fact]
    public async Task returns_ok_when_successful()
    {
        var taskListId = Guid.NewGuid();
        var task = Guid.NewGuid().ToString();
        var commandExecutor = new FakeCommandExecutor();

        var result = await CompleteTaskHandler.Post(taskListId, task, commandExecutor, CancellationToken.None);

        var response = Assert.IsType<Ok<CompleteTaskHandler.Response>>(result);
        Assert.NotNull(response.Value);
        Assert.Equal(taskListId, response.Value.TaskListId);
        Assert.Equal(task, response.Value.Task);
    }

    [Fact]
    public async Task returns_conflict_when_task_already_added()
    {
        var taskListId = Guid.NewGuid();
        var task = Guid.NewGuid().ToString();
        var commandExecutor = new FakeCommandExecutor();
        commandExecutor.Throws(new TaskAlreadyCompletedException());

        var result = await CompleteTaskHandler.Post(taskListId, task, commandExecutor, CancellationToken.None);

        Assert.IsType<Conflict>(result);
    }
}
