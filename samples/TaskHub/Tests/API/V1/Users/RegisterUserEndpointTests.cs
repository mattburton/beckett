using API.V1.Users;
using Contracts.Users.Commands;
using TaskHub.Users;

namespace Tests.API.V1.Users;

public class RegisterUserEndpointTests
{
    [Fact]
    public async Task executes_command()
    {
        var expectedCommand = new RegisterUser(Example.String, Example.String);
        var dispatcher = new FakeDispatcher();
        var module = new UserModule(dispatcher);
        var request = new RegisterUserEndpoint.Request(Example.String, Example.String);

        await RegisterUserEndpoint.Handle(request, module, CancellationToken.None);

        var actualCommand = Assert.IsType<RegisterUser>(dispatcher.Received);
        Assert.Equal(expectedCommand, actualCommand);
    }

    [Fact]
    public async Task returns_ok_when_successful()
    {
        var dispatcher = new FakeDispatcher();
        var module = new UserModule(dispatcher);
        var request = new RegisterUserEndpoint.Request(Example.String, Example.String);

        var result = await RegisterUserEndpoint.Handle(request, module, CancellationToken.None);

        Assert.IsType<Ok>(result);
    }

    [Fact]
    public async Task returns_conflict_when_stream_already_exists()
    {
        var dispatcher = new FakeDispatcher();
        var module = new UserModule(dispatcher);
        var request = new RegisterUserEndpoint.Request(Example.String, Example.String);
        dispatcher.Throws(new StreamAlreadyExistsException());

        var result = await RegisterUserEndpoint.Handle(request, module, CancellationToken.None);

        Assert.IsType<Conflict>(result);
    }
}
