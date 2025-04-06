using Users.RegisterUser;

namespace Users.Tests.RegisterUser;

public class RegisterUserEndpointTests
{
    [Fact]
    public async Task executes_command()
    {
        var expectedCommand = new RegisterUserCommand(Example.String, Example.String);
        var module = new FakeUserModule();
        var request = new RegisterUserEndpoint.Request(Example.String, Example.String);

        await RegisterUserEndpoint.Handle(request, module, CancellationToken.None);

        var actualCommand = Assert.IsType<RegisterUserCommand>(module.Received);
        Assert.Equal(expectedCommand, actualCommand);
    }

    [Fact]
    public async Task returns_ok_when_successful()
    {
        var module = new FakeUserModule();
        var request = new RegisterUserEndpoint.Request(Example.String, Example.String);

        var result = await RegisterUserEndpoint.Handle(request, module, CancellationToken.None);

        Assert.IsType<Ok>(result);
    }

    [Fact]
    public async Task returns_conflict_when_stream_already_exists()
    {
        var module = new FakeUserModule();
        var request = new RegisterUserEndpoint.Request(Example.String, Example.String);
        module.Throws(new StreamAlreadyExistsException());

        var result = await RegisterUserEndpoint.Handle(request, module, CancellationToken.None);

        Assert.IsType<Conflict>(result);
    }
}
