using TaskLists.CompleteTask;

namespace TaskLists.Tests.CompleteTask;

public class CompleteTaskEndpointTests
{
    [Fact]
    public async Task executes_command()
    {
        var expectedCommand = new CompleteTaskCommand(Example.Guid, Example.String);
        var module = new FakeTaskListModule();

        await CompleteTaskEndpoint.Handle(Example.Guid, Example.String, module, CancellationToken.None);

        var actualCommand = Assert.IsType<CompleteTaskCommand>(module.Received);
        Assert.Equal(expectedCommand, actualCommand);
    }

    [Fact]
    public async Task returns_ok_with_result_when_successful()
    {
        var module = new FakeTaskListModule();

        var result = await CompleteTaskEndpoint.Handle(Example.Guid, Example.String, module, CancellationToken.None);

        var response = Assert.IsType<Ok<CompleteTaskEndpoint.Response>>(result);
        Assert.NotNull(response.Value);
        Assert.Equal(Example.Guid, response.Value.TaskListId);
        Assert.Equal(Example.String, response.Value.Task);
    }

    [Fact]
    public async Task returns_conflict_when_task_already_added()
    {
        var module = new FakeTaskListModule();
        module.Throws(new TaskAlreadyCompletedException());

        var result = await CompleteTaskEndpoint.Handle(Example.Guid, Example.String, module, CancellationToken.None);

        Assert.IsType<Conflict>(result);
    }
}
