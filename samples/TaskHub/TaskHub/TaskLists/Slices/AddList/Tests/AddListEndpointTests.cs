namespace TaskHub.TaskLists.Slices.AddList.Tests;

public class AddListEndpointTests
{
    [Fact]
    public async Task executes_command()
    {
        var id = Generate.Guid();
        var name = Generate.String();
        var expectedStreamName = TaskListModule.StreamName(id);
        var expectedCommand = new AddListCommand(id, name);
        var expectedVersion = ExpectedVersion.StreamDoesNotExist;
        var commandExecutor = new FakeCommandExecutor();
        var request = new AddListEndpoint.Request(id, name);

        await AddListEndpoint.Handle(request, commandExecutor, CancellationToken.None);

        Assert.NotNull(commandExecutor.Received);
        Assert.Equal(expectedStreamName, commandExecutor.Received.StreamName);
        Assert.Equal(expectedCommand, commandExecutor.Received.Command);
        Assert.NotNull(commandExecutor.Received.Options);
        Assert.Equal(expectedVersion, commandExecutor.Received.Options.ExpectedVersion);
    }

    [Fact]
    public async Task returns_ok_with_result_when_successful()
    {
        var id = Generate.Guid();
        var name = Generate.String();
        var commandExecutor = new FakeCommandExecutor();
        var request = new AddListEndpoint.Request(id, name);

        var result = await AddListEndpoint.Handle(request, commandExecutor, CancellationToken.None);

        var response = Assert.IsType<Ok<AddListEndpoint.Response>>(result);
        Assert.NotNull(response.Value);
        Assert.Equal(id, response.Value.Id);
        Assert.Equal(name, response.Value.Name);
    }

    [Fact]
    public async Task returns_conflict_when_stream_already_exists()
    {
        var id = Generate.Guid();
        var name = Generate.String();
        var commandExecutor = new FakeCommandExecutor();
        var request = new AddListEndpoint.Request(id, name);
        commandExecutor.Throws(new StreamAlreadyExistsException());

        var result = await AddListEndpoint.Handle(request, commandExecutor, CancellationToken.None);

        Assert.IsType<Conflict>(result);
    }
}
