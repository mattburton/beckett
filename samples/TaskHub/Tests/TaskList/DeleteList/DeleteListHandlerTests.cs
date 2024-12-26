using TaskHub.TaskList.DeleteList;

namespace Tests.TaskList.DeleteList;

public class DeleteListHandlerTests
{
    [Fact]
    public async Task executes_add_list_command()
    {
        var id = Guid.NewGuid();
        var expectedStreamName = TaskHub.TaskList.TaskList.StreamName(id);
        var expectedCommand = new DeleteListCommand(id);
        var expectedVersion = ExpectedVersion.StreamExists;
        var commandExecutor = new FakeCommandExecutor();

        await DeleteListHandler.Delete(id, commandExecutor, CancellationToken.None);

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
        var commandExecutor = new FakeCommandExecutor();

        var result = await DeleteListHandler.Delete(id, commandExecutor, CancellationToken.None);

        Assert.IsType<Ok>(result);
    }

    [Fact]
    public async Task returns_conflict_when_stream_does_not_exist()
    {
        var id = Guid.NewGuid();
        var commandExecutor = new FakeCommandExecutor();
        commandExecutor.Throws(new StreamDoesNotExistException());

        var result = await DeleteListHandler.Delete(id, commandExecutor, CancellationToken.None);

        Assert.IsType<Conflict>(result);
    }
}
