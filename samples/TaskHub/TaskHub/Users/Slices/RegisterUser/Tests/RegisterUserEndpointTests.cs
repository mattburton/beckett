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
        var commandBus = new FakeCommandBus();
        var request = new RegisterUserEndpoint.Request(username, email);

        await RegisterUserEndpoint.Handle(request, commandBus, CancellationToken.None);

        var actualCommand = Assert.IsType<RegisterUserCommand>(commandBus.Received);
        Assert.Equal(expectedStreamName, actualCommand.StreamName());
        Assert.Equal(expectedCommand, actualCommand);
        Assert.Equal(expectedVersion, actualCommand.ExpectedVersion);
    }

    [Fact]
    public async Task returns_ok_when_successful()
    {
        var username = Generate.String();
        var email = Generate.String();
        var commandBus = new FakeCommandBus();
        var request = new RegisterUserEndpoint.Request(username, email);

        var result = await RegisterUserEndpoint.Handle(request, commandBus, CancellationToken.None);

        Assert.IsType<Ok>(result);
    }

    [Fact]
    public async Task returns_conflict_when_stream_already_exists()
    {
        var username = Generate.String();
        var email = Generate.String();
        var commandBus = new FakeCommandBus();
        var request = new RegisterUserEndpoint.Request(username, email);
        commandBus.Throws(new StreamAlreadyExistsException());

        var result = await RegisterUserEndpoint.Handle(request, commandBus, CancellationToken.None);

        Assert.IsType<Conflict>(result);
    }
}
