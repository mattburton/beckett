namespace TaskHub.Users.Slices.RegisterUser.Tests;

public class RegisterUserEndpointTests
{
    [Fact]
    public async Task executes_command()
    {
        var username = Generate.String();
        var email = Generate.String();
        var expectedStreamName = UserModule.StreamName(username);
        var expectedCommand = new RegisterUserCommand(username, email);
        var expectedVersion = ExpectedVersion.StreamDoesNotExist;
        var commandExecutor = new FakeCommandExecutor();
        var request = new RegisterUserEndpoint.Request(username, email);

        await RegisterUserEndpoint.Handle(request, commandExecutor, CancellationToken.None);

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
        var email = Generate.String();
        var commandExecutor = new FakeCommandExecutor();
        var request = new RegisterUserEndpoint.Request(username, email);

        var result = await RegisterUserEndpoint.Handle(request, commandExecutor, CancellationToken.None);

        Assert.IsType<Ok>(result);
    }

    [Fact]
    public async Task returns_conflict_when_stream_already_exists()
    {
        var username = Generate.String();
        var email = Generate.String();
        var commandExecutor = new FakeCommandExecutor();
        var request = new RegisterUserEndpoint.Request(username, email);
        commandExecutor.Throws(new StreamAlreadyExistsException());

        var result = await RegisterUserEndpoint.Handle(request, commandExecutor, CancellationToken.None);

        Assert.IsType<Conflict>(result);
    }
}
