namespace TaskHub.TaskLists.Slices.DeleteTaskList.Tests;

public class DeleteTaskListEndpointTests
{
    [Fact]
    public async Task executes_command()
    {
        var id = Generate.Guid();
        var expectedStreamName = TaskListModule.StreamName(id);
        var expectedCommand = new DeleteTaskListCommand(id);
        var expectedVersion = ExpectedVersion.StreamExists;
        var commandExecutor = new FakeCommandExecutor();

        await DeleteTaskListEndpoint.Handle(id, commandExecutor, CancellationToken.None);

        Assert.NotNull(commandExecutor.Received);
        Assert.Equal(expectedStreamName, commandExecutor.Received.StreamName);
        Assert.Equal(expectedCommand, commandExecutor.Received.Command);
        Assert.NotNull(commandExecutor.Received.Options);
        Assert.Equal(expectedVersion, commandExecutor.Received.Options.ExpectedVersion);
    }

    [Fact]
    public async Task returns_ok_when_successful()
    {
        var id = Generate.Guid();
        var commandExecutor = new FakeCommandExecutor();

        var result = await DeleteTaskListEndpoint.Handle(id, commandExecutor, CancellationToken.None);

        Assert.IsType<Ok>(result);
    }

    [Fact]
    public async Task returns_conflict_when_stream_does_not_exist()
    {
        var id = Generate.Guid();
        var commandExecutor = new FakeCommandExecutor();
        commandExecutor.Throws(new StreamDoesNotExistException());

        var result = await DeleteTaskListEndpoint.Handle(id, commandExecutor, CancellationToken.None);

        Assert.IsType<Conflict>(result);
    }
}
