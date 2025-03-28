using API.V1.TaskLists;
using Contracts.TaskLists.Commands;
using TaskHub.TaskLists;

namespace Tests.API.V1.TaskLists;

public class ChangeTaskListNameEndpointTests
{
    [Fact]
    public async Task executes_command()
    {
        var taskListId = Generate.Guid();
        var name = Generate.String();
        var expectedCommand = new ChangeTaskListNameCommand(taskListId, name);
        var commandDispatcher = new FakeCommandDispatcher();
        var queryDispatcher = new FakeQueryDispatcher();
        var module = new TaskListModule(commandDispatcher, queryDispatcher);
        var request = new ChangeTaskListNameEndpoint.Request(name);

        await ChangeTaskListNameEndpoint.Handle(taskListId, request, module, CancellationToken.None);

        var actualCommand = Assert.IsType<ChangeTaskListNameCommand>(commandDispatcher.Received);
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
        var request = new ChangeTaskListNameEndpoint.Request(name);

        var result = await ChangeTaskListNameEndpoint.Handle(taskListId, request, module, CancellationToken.None);

        var response = Assert.IsType<Ok<ChangeTaskListNameEndpoint.Response>>(result);
        Assert.NotNull(response.Value);
        Assert.Equal(taskListId, response.Value.Id);
        Assert.Equal(name, response.Value.Name);
    }
}
