using API.V1.TaskLists;
using Contracts.TaskLists.Commands;
using TaskHub.TaskLists;

namespace Tests.API.V1.TaskLists;

public class ChangeTaskListNameEndpointTests
{
    [Fact]
    public async Task executes_command()
    {
        var expectedCommand = new ChangeTaskListName(Example.Guid, Example.String);
        var dispatcher = new FakeDispatcher();
        var module = new TaskListModule(dispatcher);
        var request = new ChangeTaskListNameEndpoint.Request(Example.String);

        await ChangeTaskListNameEndpoint.Handle(Example.Guid, request, module, CancellationToken.None);

        var actualCommand = Assert.IsType<ChangeTaskListName>(dispatcher.Received);
        Assert.Equal(expectedCommand, actualCommand);
    }

    [Fact]
    public async Task returns_ok_with_result_when_successful()
    {
        var dispatcher = new FakeDispatcher();
        var module = new TaskListModule(dispatcher);
        var request = new ChangeTaskListNameEndpoint.Request(Example.String);

        var result = await ChangeTaskListNameEndpoint.Handle(Example.Guid, request, module, CancellationToken.None);

        var response = Assert.IsType<Ok<ChangeTaskListNameEndpoint.Response>>(result);
        Assert.NotNull(response.Value);
        Assert.Equal(Example.Guid, response.Value.Id);
        Assert.Equal(Example.String, response.Value.Name);
    }
}
