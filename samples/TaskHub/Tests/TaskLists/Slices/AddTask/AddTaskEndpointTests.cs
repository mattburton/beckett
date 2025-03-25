using TaskHub.TaskLists;
using TaskHub.TaskLists.Slices.AddTask;

namespace Tests.TaskLists.Slices.AddTask;

public class AddTaskEndpointTests
{
    [Fact]
    public async Task executes_command()
    {
        var taskListId = Generate.Guid();
        var name = Generate.String();
        var expectedStreamName = TaskListModule.StreamName(taskListId);
        var expectedCommand = new AddTaskCommand(taskListId, name);
        var commandBus = new FakeCommandBus();
        var request = new AddTaskEndpoint.Request(name);

        await AddTaskEndpoint.Handle(taskListId, request, commandBus, CancellationToken.None);

        var actualCommand = Assert.IsType<AddTaskCommand>(commandBus.Received);
        Assert.Equal(expectedStreamName, actualCommand.StreamName());
        Assert.Equal(expectedCommand, actualCommand);
    }

    [Fact]
    public async Task returns_ok_with_result_when_successful()
    {
        var taskListId = Generate.Guid();
        var name = Generate.String();
        var commandBus = new FakeCommandBus();
        var request = new AddTaskEndpoint.Request(name);

        var result = await AddTaskEndpoint.Handle(taskListId, request, commandBus, CancellationToken.None);

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
        var commandBus = new FakeCommandBus();
        var request = new AddTaskEndpoint.Request(name);
        commandBus.Throws(new TaskAlreadyAddedException());

        var result = await AddTaskEndpoint.Handle(taskListId, request, commandBus, CancellationToken.None);

        Assert.IsType<Conflict>(result);
    }
}
