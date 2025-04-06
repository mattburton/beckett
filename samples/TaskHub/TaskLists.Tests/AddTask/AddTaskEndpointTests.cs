using TaskLists.AddTask;

namespace TaskLists.Tests.AddTask;

public class AddTaskEndpointTests
{
    [Fact]
    public async Task executes_command()
    {
        var expectedCommand = new AddTaskCommand(Example.Guid, Example.String);
        var module = new FakeTaskListModule();
        var request = new AddTaskEndpoint.Request(Example.String);

        await AddTaskEndpoint.Handle(Example.Guid, request, module, CancellationToken.None);

        var actualCommand = Assert.IsType<AddTaskCommand>(module.Received);
        Assert.Equal(expectedCommand, actualCommand);
    }

    [Fact]
    public async Task returns_ok_with_result_when_successful()
    {
        var module = new FakeTaskListModule();
        var request = new AddTaskEndpoint.Request(Example.String);

        var result = await AddTaskEndpoint.Handle(Example.Guid, request, module, CancellationToken.None);

        var response = Assert.IsType<Ok<AddTaskEndpoint.Response>>(result);
        Assert.NotNull(response.Value);
        Assert.Equal(Example.Guid, response.Value.TaskListId);
        Assert.Equal(Example.String, response.Value.Task);
    }

    [Fact]
    public async Task returns_conflict_when_task_already_added()
    {
        var module = new FakeTaskListModule();
        var request = new AddTaskEndpoint.Request(Example.String);
        module.Throws(new TaskAlreadyAddedException());

        var result = await AddTaskEndpoint.Handle(Example.Guid, request, module, CancellationToken.None);

        Assert.IsType<Conflict>(result);
    }
}
