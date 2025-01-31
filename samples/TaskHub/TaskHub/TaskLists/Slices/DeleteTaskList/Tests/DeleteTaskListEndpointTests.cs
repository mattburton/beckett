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
        var commandBus = new FakeCommandBus();

        await DeleteTaskListEndpoint.Handle(id, commandBus, CancellationToken.None);

        var actualCommand = Assert.IsType<DeleteTaskListCommand>(commandBus.Received);
        Assert.Equal(expectedStreamName, actualCommand.StreamName());
        Assert.Equal(expectedCommand, actualCommand);
        Assert.Equal(expectedVersion, actualCommand.ExpectedVersion);
    }

    [Fact]
    public async Task returns_ok_when_successful()
    {
        var id = Generate.Guid();
        var commandBus = new FakeCommandBus();

        var result = await DeleteTaskListEndpoint.Handle(id, commandBus, CancellationToken.None);

        Assert.IsType<Ok>(result);
    }

    [Fact]
    public async Task returns_conflict_when_stream_does_not_exist()
    {
        var id = Generate.Guid();
        var commandBus = new FakeCommandBus();
        commandBus.Throws(new StreamDoesNotExistException());

        var result = await DeleteTaskListEndpoint.Handle(id, commandBus, CancellationToken.None);

        Assert.IsType<Conflict>(result);
    }
}
