using API.V1.Users;
using Contracts.Users.Commands;
using TaskHub.Users;

namespace Tests.API.V1.Users;

public class RegisterUserEndpointTests
{
    [Fact]
    public async Task executes_command()
    {
        var username = Generate.String();
        var email = Generate.String();
        var expectedCommand = new RegisterUserCommand(username, email);
        var commandDispatcher = new FakeCommandDispatcher();
        var queryDispatcher = new FakeQueryDispatcher();
        var module = new UserModule(commandDispatcher, queryDispatcher);
        var request = new RegisterUserEndpoint.Request(username, email);

        await RegisterUserEndpoint.Handle(request, module, CancellationToken.None);

        var actualCommand = Assert.IsType<RegisterUserCommand>(commandDispatcher.Received);
        Assert.Equal(expectedCommand, actualCommand);
    }

    [Fact]
    public async Task returns_ok_when_successful()
    {
        var username = Generate.String();
        var email = Generate.String();
        var commandDispatcher = new FakeCommandDispatcher();
        var queryDispatcher = new FakeQueryDispatcher();
        var module = new UserModule(commandDispatcher, queryDispatcher);
        var request = new RegisterUserEndpoint.Request(username, email);

        var result = await RegisterUserEndpoint.Handle(request, module, CancellationToken.None);

        Assert.IsType<Ok>(result);
    }

    [Fact]
    public async Task returns_conflict_when_stream_already_exists()
    {
        var username = Generate.String();
        var email = Generate.String();
        var commandDispatcher = new FakeCommandDispatcher();
        var queryDispatcher = new FakeQueryDispatcher();
        var module = new UserModule(commandDispatcher, queryDispatcher);
        var request = new RegisterUserEndpoint.Request(username, email);
        commandDispatcher.Throws(new StreamAlreadyExistsException());

        var result = await RegisterUserEndpoint.Handle(request, module, CancellationToken.None);

        Assert.IsType<Conflict>(result);
    }
}
