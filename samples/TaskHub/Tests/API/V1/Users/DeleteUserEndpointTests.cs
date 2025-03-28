using API.V1.Users;
using Contracts.Users.Commands;
using TaskHub.Users;

namespace Tests.API.V1.Users;

public class DeleteUserEndpointTests
{
    [Fact]
    public async Task executes_command()
    {
        var username = Generate.String();
        var expectedCommand = new DeleteUserCommand(username);
        var commandDispatcher = new FakeCommandDispatcher();
        var queryDispatcher = new FakeQueryDispatcher();
        var module = new UserModule(commandDispatcher, queryDispatcher);

        await DeleteUserEndpoint.Handle(username, module, CancellationToken.None);

        var actualCommand = Assert.IsType<DeleteUserCommand>(commandDispatcher.Received);
        Assert.Equal(expectedCommand, actualCommand);
    }

    [Fact]
    public async Task returns_ok_when_successful()
    {
        var username = Generate.String();
        var commandDispatcher = new FakeCommandDispatcher();
        var queryDispatcher = new FakeQueryDispatcher();
        var module = new UserModule(commandDispatcher, queryDispatcher);

        var result = await DeleteUserEndpoint.Handle(username, module, CancellationToken.None);

        Assert.IsType<Ok>(result);
    }

    [Fact]
    public async Task returns_conflict_when_stream_does_not_exist()
    {
        var username = Generate.String();
        var commandDispatcher = new FakeCommandDispatcher();
        var queryDispatcher = new FakeQueryDispatcher();
        var module = new UserModule(commandDispatcher, queryDispatcher);
        commandDispatcher.Throws(new StreamDoesNotExistException());

        var result = await DeleteUserEndpoint.Handle(username, module, CancellationToken.None);

        Assert.IsType<Conflict>(result);
    }
}
