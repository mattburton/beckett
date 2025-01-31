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
        var commandBus = new FakeCommandBus();

        await DeleteUserEndpoint.Handle(username, commandBus, CancellationToken.None);

        var actualCommand = Assert.IsType<DeleteUserCommand>(commandBus.Received);
        Assert.Equal(expectedStreamName, actualCommand.StreamName());
        Assert.Equal(expectedCommand, actualCommand);
        Assert.Equal(expectedVersion, actualCommand.ExpectedVersion);
    }

    [Fact]
    public async Task returns_ok_when_successful()
    {
        var username = Generate.String();
        var commandBus = new FakeCommandBus();

        var result = await DeleteUserEndpoint.Handle(username, commandBus, CancellationToken.None);

        Assert.IsType<Ok>(result);
    }

    [Fact]
    public async Task returns_conflict_when_stream_does_not_exist()
    {
        var username = Generate.String();
        var commandBus = new FakeCommandBus();
        commandBus.Throws(new StreamDoesNotExistException());

        var result = await DeleteUserEndpoint.Handle(username, commandBus, CancellationToken.None);

        Assert.IsType<Conflict>(result);
    }
}
