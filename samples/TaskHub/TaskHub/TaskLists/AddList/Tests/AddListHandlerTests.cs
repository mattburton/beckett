namespace TaskHub.TaskLists.AddList.Tests;

public class AddListHandlerTests
{
    [Fact]
    public async Task executes_add_list_command()
    {
        var id = Guid.NewGuid();
        var name = Guid.NewGuid().ToString();
        var expectedStreamName = TaskList.StreamName(id);
        var expectedCommand = new AddListCommand(id, name);
        var expectedVersion = ExpectedVersion.StreamDoesNotExist;
        var commandExecutor = new FakeCommandExecutor();
        var request = new AddListHandler.Request(id, name);

        await AddListHandler.Post(request, commandExecutor, CancellationToken.None);

        Assert.True(commandExecutor.Executed);
        Assert.Equal(expectedStreamName, commandExecutor.ReceivedStreamName);
        Assert.Equal(expectedCommand, commandExecutor.ReceivedCommand);
        Assert.NotNull(commandExecutor.ReceivedOptions);
        Assert.Equal(expectedVersion, commandExecutor.ReceivedOptions.ExpectedVersion);
    }

    [Fact]
    public async Task returns_ok_when_successful()
    {
        var id = Guid.NewGuid();
        var name = Guid.NewGuid().ToString();
        var commandExecutor = new FakeCommandExecutor();
        var request = new AddListHandler.Request(id, name);

        var result = await AddListHandler.Post(request, commandExecutor, CancellationToken.None);

        var response = Assert.IsType<Ok<AddListHandler.Response>>(result);
        Assert.NotNull(response.Value);
        Assert.Equal(id, response.Value.Id);
        Assert.Equal(name, response.Value.Name);
    }

    [Fact]
    public async Task returns_conflict_when_stream_already_exists()
    {
        var id = Guid.NewGuid();
        var name = Guid.NewGuid().ToString();
        var commandExecutor = new FakeCommandExecutor();
        var request = new AddListHandler.Request(id, name);
        commandExecutor.Throws(new StreamAlreadyExistsException());

        var result = await AddListHandler.Post(request, commandExecutor, CancellationToken.None);

        Assert.IsType<Conflict>(result);
    }
}
