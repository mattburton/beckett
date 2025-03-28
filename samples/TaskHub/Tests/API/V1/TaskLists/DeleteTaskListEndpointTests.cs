using API.V1.TaskLists;
using Contracts.TaskLists.Commands;
using Core.Contracts;
using TaskHub.TaskLists;

namespace Tests.API.V1.TaskLists;

public class DeleteTaskListEndpointTests
{
    [Fact]
    public async Task executes_command()
    {
        var id = Generate.Guid();
        var expectedCommand = new DeleteTaskListCommand(id);
        var commandDispatcher = new FakeCommandDispatcher();
        var queryDispatcher = new FakeQueryDispatcher();
        var module = new TaskListModule(commandDispatcher, queryDispatcher);

        await DeleteTaskListEndpoint.Handle(id, module, CancellationToken.None);

        var actualCommand = Assert.IsType<DeleteTaskListCommand>(commandDispatcher.Received);
        Assert.Equal(expectedCommand, actualCommand);
    }

    [Fact]
    public async Task returns_ok_when_successful()
    {
        var id = Generate.Guid();
        var commandDispatcher = new FakeCommandDispatcher();
        var queryDispatcher = new FakeQueryDispatcher();
        var module = new TaskListModule(commandDispatcher, queryDispatcher);

        var result = await DeleteTaskListEndpoint.Handle(id, module, CancellationToken.None);

        Assert.IsType<Ok>(result);
    }

    [Fact]
    public async Task returns_conflict_when_task_list_does_not_exist()
    {
        var id = Generate.Guid();
        var commandDispatcher = new FakeCommandDispatcher();
        var queryDispatcher = new FakeQueryDispatcher();
        var module = new TaskListModule(commandDispatcher, queryDispatcher);
        commandDispatcher.Throws(new ResourceNotFoundException());

        var result = await DeleteTaskListEndpoint.Handle(id, module, CancellationToken.None);

        Assert.IsType<Conflict>(result);
    }
}
