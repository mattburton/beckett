namespace TaskHub.TaskLists.Slices.ChangeListName.Tests;

public class ChangeListNameEndpointTests
{
    [Fact]
    public async Task executes_command()
    {
        var taskListId = Generate.Guid();
        var name = Generate.String();
        var expectedStreamName = TaskListModule.StreamName(taskListId);
        var expectedCommand = new ChangeListNameCommand(taskListId, name);
        var commandExecutor = new FakeCommandExecutor();
        var request = new ChangeListNameEndpoint.Request(name);

        await ChangeListNameEndpoint.Handle(taskListId, request, commandExecutor, CancellationToken.None);

        Assert.NotNull(commandExecutor.Received);
        Assert.Equal(expectedStreamName, commandExecutor.Received.StreamName);
        Assert.Equal(expectedCommand, commandExecutor.Received.Command);
    }

    [Fact]
    public async Task returns_ok_with_result_when_successful()
    {
        var taskListId = Generate.Guid();
        var name = Generate.String();
        var commandExecutor = new FakeCommandExecutor();
        var request = new ChangeListNameEndpoint.Request(name);

        var result = await ChangeListNameEndpoint.Handle(taskListId, request, commandExecutor, CancellationToken.None);

        var response = Assert.IsType<Ok<ChangeListNameEndpoint.Response>>(result);
        Assert.NotNull(response.Value);
        Assert.Equal(taskListId, response.Value.Id);
        Assert.Equal(name, response.Value.Name);
    }
}
