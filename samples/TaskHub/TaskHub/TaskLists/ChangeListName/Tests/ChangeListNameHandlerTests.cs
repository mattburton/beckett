namespace TaskHub.TaskLists.ChangeListName.Tests;

public class ChangeListNameHandlerTests
{
    [Fact]
    public async Task executes_change_list_name_command()
    {
        var taskListId = Guid.NewGuid();
        var name = Guid.NewGuid().ToString();
        var expectedStreamName = TaskList.StreamName(taskListId);
        var expectedCommand = new ChangeListNameCommand(taskListId, name);
        var commandExecutor = new FakeCommandExecutor();
        var request = new ChangeListNameHandler.Request(name);

        await ChangeListNameHandler.Post(taskListId, request, commandExecutor, CancellationToken.None);

        Assert.True(commandExecutor.Executed);
        Assert.Equal(expectedStreamName, commandExecutor.ReceivedStreamName);
        Assert.Equal(expectedCommand, commandExecutor.ReceivedCommand);
    }

    [Fact]
    public async Task returns_ok_when_successful()
    {
        var taskListId = Guid.NewGuid();
        var name = Guid.NewGuid().ToString();
        var commandExecutor = new FakeCommandExecutor();
        var request = new ChangeListNameHandler.Request(name);

        var result = await ChangeListNameHandler.Post(taskListId, request, commandExecutor, CancellationToken.None);

        var response = Assert.IsType<Ok<ChangeListNameHandler.Response>>(result);
        Assert.NotNull(response.Value);
        Assert.Equal(taskListId, response.Value.Id);
        Assert.Equal(name, response.Value.Name);
    }
}
