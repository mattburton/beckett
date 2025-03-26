using API.V1.TaskLists;
using TaskHub.TaskLists;
using TaskHub.TaskLists.Slices.ChangeTaskListName;

namespace Tests.API.V1.TaskLists;

public class ChangeTaskListNameEndpointTests
{
    [Fact]
    public async Task executes_command()
    {
        var taskListId = Generate.Guid();
        var name = Generate.String();
        var expectedStreamName = TaskListModule.StreamName(taskListId);
        var expectedCommand = new ChangeTaskListNameCommand(taskListId, name);
        var commandBus = new FakeCommandBus();
        var request = new ChangeTaskListNameEndpoint.Request(name);

        await ChangeTaskListNameEndpoint.Handle(taskListId, request, commandBus, CancellationToken.None);

        var actualCommand = Assert.IsType<ChangeTaskListNameCommand>(commandBus.Received);
        Assert.Equal(expectedStreamName, actualCommand.StreamName());
        Assert.Equal(expectedCommand, actualCommand);
    }

    [Fact]
    public async Task returns_ok_with_result_when_successful()
    {
        var taskListId = Generate.Guid();
        var name = Generate.String();
        var commandBus = new FakeCommandBus();
        var request = new ChangeTaskListNameEndpoint.Request(name);

        var result = await ChangeTaskListNameEndpoint.Handle(taskListId, request, commandBus, CancellationToken.None);

        var response = Assert.IsType<Ok<ChangeTaskListNameEndpoint.Response>>(result);
        Assert.NotNull(response.Value);
        Assert.Equal(taskListId, response.Value.Id);
        Assert.Equal(name, response.Value.Name);
    }
}
