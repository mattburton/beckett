namespace TaskHub.Users.Slices.DeleteUser.Tests;

public class DeleteUserEndpointTests
{
    [Fact]
    public async Task executes_command()
    {
        var username = Generate.String();
        var expectedStreamName = UserModule.StreamName(username);
        var expectedCommand = new DeleteUserCommand(username);
        var expectedVersion = ExpectedVersion.StreamExists;
        var commandExecutor = new FakeCommandExecutor();

        await DeleteUserEndpoint.Handle(username, commandExecutor, CancellationToken.None);

        Assert.NotNull(commandExecutor.Received);
        Assert.Equal(expectedStreamName, commandExecutor.Received.StreamName);
        Assert.Equal(expectedCommand, commandExecutor.Received.Command);
        Assert.NotNull(commandExecutor.Received.Options);
        Assert.Equal(expectedVersion, commandExecutor.Received.Options.ExpectedVersion);
    }

    [Fact]
    public async Task returns_ok_when_successful()
    {
        var username = Generate.String();
        var commandExecutor = new FakeCommandExecutor();

        var result = await DeleteUserEndpoint.Handle(username, commandExecutor, CancellationToken.None);

        Assert.IsType<Ok>(result);
    }

    [Fact]
    public async Task returns_conflict_when_stream_does_not_exist()
    {
        var username = Generate.String();
        var commandExecutor = new FakeCommandExecutor();
        commandExecutor.Throws(new StreamDoesNotExistException());

        var result = await DeleteUserEndpoint.Handle(username, commandExecutor, CancellationToken.None);

        Assert.IsType<Conflict>(result);
    }
}
