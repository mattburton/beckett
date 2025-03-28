using API.V1.TaskLists;
using Contracts.TaskLists.Commands;
using Contracts.TaskLists.Exceptions;
using TaskHub.TaskLists;

namespace Tests.API.V1.TaskLists;

public class AddTaskEndpointTests
{
    [Fact]
    public async Task executes_command()
    {
        var taskListId = Generate.Guid();
        var name = Generate.String();
        var expectedCommand = new AddTaskCommand(taskListId, name);
        var commandDispatcher = new FakeCommandDispatcher();
        var queryDispatcher = new FakeQueryDispatcher();
        var module = new TaskListModule(commandDispatcher, queryDispatcher);
        var request = new AddTaskEndpoint.Request(name);

        await AddTaskEndpoint.Handle(taskListId, request, module, CancellationToken.None);

        var actualCommand = Assert.IsType<AddTaskCommand>(commandDispatcher.Received);
        Assert.Equal(expectedCommand, actualCommand);
    }

    [Fact]
    public async Task returns_ok_with_result_when_successful()
    {
        var taskListId = Generate.Guid();
        var name = Generate.String();
        var commandDispatcher = new FakeCommandDispatcher();
        var queryDispatcher = new FakeQueryDispatcher();
        var module = new TaskListModule(commandDispatcher, queryDispatcher);
        var request = new AddTaskEndpoint.Request(name);

        var result = await AddTaskEndpoint.Handle(taskListId, request, module, CancellationToken.None);

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
        var commandDispatcher = new FakeCommandDispatcher();
        var queryDispatcher = new FakeQueryDispatcher();
        var module = new TaskListModule(commandDispatcher, queryDispatcher);
        var request = new AddTaskEndpoint.Request(name);
        commandDispatcher.Throws(new TaskAlreadyAddedException());

        var result = await AddTaskEndpoint.Handle(taskListId, request, module, CancellationToken.None);

        Assert.IsType<Conflict>(result);
    }
}
