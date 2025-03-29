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
        var dispatcher = new FakeDispatcher();
        var module = new TaskListModule(dispatcher);
        var expectedCommand = new AddTaskList(Example.Guid, Example.String);
        var request = new AddTaskListEndpoint.Request(Example.Guid, Example.String);

        await AddTaskListEndpoint.Handle(request, module, CancellationToken.None);

        var actualCommand = Assert.IsType<AddTaskList>(dispatcher.Received);
        Assert.Equal(expectedCommand, actualCommand);
    }

    [Fact]
    public async Task returns_ok_with_result_when_successful()
    {
        var dispatcher = new FakeDispatcher();
        var module = new TaskListModule(dispatcher);
        var request = new AddTaskListEndpoint.Request(Example.Guid, Example.String);

        var result = await AddTaskListEndpoint.Handle(request, module, CancellationToken.None);

        var response = Assert.IsType<Ok<AddTaskListEndpoint.Response>>(result);
        Assert.NotNull(response.Value);
        Assert.Equal(Example.Guid, response.Value.Id);
        Assert.Equal(Example.String, response.Value.Name);
    }

    [Fact]
    public async Task returns_conflict_when_task_list_already_exists()
    {
        var dispatcher = new FakeDispatcher();
        var module = new TaskListModule(dispatcher);
        var request = new AddTaskListEndpoint.Request(Example.Guid, Example.String);
        dispatcher.Throws(new ResourceAlreadyExistsException());

        var result = await AddTaskListEndpoint.Handle(request, module, CancellationToken.None);

        Assert.IsType<Conflict>(result);
    }
}
