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
        var expectedCommand = new DeleteTaskList(Example.Guid);
        var dispatcher = new FakeDispatcher();
        var module = new TaskListModule(dispatcher);

        await DeleteTaskListEndpoint.Handle(Example.Guid, module, CancellationToken.None);

        var actualCommand = Assert.IsType<DeleteTaskList>(dispatcher.Received);
        Assert.Equal(expectedCommand, actualCommand);
    }

    [Fact]
    public async Task returns_ok_when_successful()
    {
        var dispatcher = new FakeDispatcher();
        var module = new TaskListModule(dispatcher);

        var result = await DeleteTaskListEndpoint.Handle(Example.Guid, module, CancellationToken.None);

        Assert.IsType<Ok>(result);
    }

    [Fact]
    public async Task returns_conflict_when_task_list_does_not_exist()
    {
        var dispatcher = new FakeDispatcher();
        var module = new TaskListModule(dispatcher);
        dispatcher.Throws(new ResourceNotFoundException());

        var result = await DeleteTaskListEndpoint.Handle(Example.Guid, module, CancellationToken.None);

        Assert.IsType<Conflict>(result);
    }
}
