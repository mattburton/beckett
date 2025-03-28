using API.V1.TaskLists;
using Contracts.TaskLists.Commands;
using Core.Contracts;
using TaskHub.TaskLists;

namespace Tests.API.V1.TaskLists;

public class AddTaskListEndpointTests
{
    [Fact]
    public async Task executes_command()
    {
        var id = Generate.Guid();
        var name = Generate.String();
        var commandDispatcher = new FakeCommandDispatcher();
        var queryDispatcher = new FakeQueryDispatcher();
        var module = new TaskListModule(commandDispatcher, queryDispatcher);
        var expectedCommand = new AddTaskListCommand(id, name);
        var request = new AddTaskListEndpoint.Request(id, name);

        await AddTaskListEndpoint.Handle(request, module, CancellationToken.None);

        var actualCommand = Assert.IsType<AddTaskListCommand>(commandDispatcher.Received);
        Assert.Equal(expectedCommand, actualCommand);
    }

    [Fact]
    public async Task returns_ok_with_result_when_successful()
    {
        var id = Generate.Guid();
        var name = Generate.String();
        var commandDispatcher = new FakeCommandDispatcher();
        var queryDispatcher = new FakeQueryDispatcher();
        var module = new TaskListModule(commandDispatcher, queryDispatcher);
        var request = new AddTaskListEndpoint.Request(id, name);

        var result = await AddTaskListEndpoint.Handle(request, module, CancellationToken.None);

        var response = Assert.IsType<Ok<AddTaskListEndpoint.Response>>(result);
        Assert.NotNull(response.Value);
        Assert.Equal(id, response.Value.Id);
        Assert.Equal(name, response.Value.Name);
    }

    [Fact]
    public async Task returns_conflict_when_task_list_already_exists()
    {
        var id = Generate.Guid();
        var name = Generate.String();
        var commandDispatcher = new FakeCommandDispatcher();
        var queryDispatcher = new FakeQueryDispatcher();
        var module = new TaskListModule(commandDispatcher, queryDispatcher);
        var request = new AddTaskListEndpoint.Request(id, name);
        commandDispatcher.Throws(new ResourceAlreadyExistsException());

        var result = await AddTaskListEndpoint.Handle(request, module, CancellationToken.None);

        Assert.IsType<Conflict>(result);
    }
}
