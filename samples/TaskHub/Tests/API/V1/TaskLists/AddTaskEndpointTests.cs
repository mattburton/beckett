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
        var expectedCommand = new AddTask(Example.Guid, Example.String);
        var dispatcher = new FakeDispatcher();
        var module = new TaskListModule(dispatcher);
        var request = new AddTaskEndpoint.Request(Example.String);

        await AddTaskEndpoint.Handle(Example.Guid, request, module, CancellationToken.None);

        var actualCommand = Assert.IsType<AddTask>(dispatcher.Received);
        Assert.Equal(expectedCommand, actualCommand);
    }

    [Fact]
    public async Task returns_ok_with_result_when_successful()
    {
        var dispatcher = new FakeDispatcher();
        var module = new TaskListModule(dispatcher);
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
        var dispatcher = new FakeDispatcher();
        var module = new TaskListModule(dispatcher);
        var request = new AddTaskEndpoint.Request(Example.String);
        dispatcher.Throws(new TaskAlreadyAddedException());

        var result = await AddTaskEndpoint.Handle(Example.Guid, request, module, CancellationToken.None);

        Assert.IsType<Conflict>(result);
    }
}
