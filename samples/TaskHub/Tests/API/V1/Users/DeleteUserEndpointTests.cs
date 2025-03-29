using API.V1.Users;
using Contracts.Users.Commands;
using TaskHub.Users;

namespace Tests.API.V1.Users;

public class DeleteUserEndpointTests
{
    [Fact]
    public async Task executes_command()
    {
        var expectedCommand = new DeleteUser(Example.String);
        var dispatcher = new FakeDispatcher();
        var module = new UserModule(dispatcher);

        await DeleteUserEndpoint.Handle(Example.String, module, CancellationToken.None);

        var actualCommand = Assert.IsType<DeleteUser>(dispatcher.Received);
        Assert.Equal(expectedCommand, actualCommand);
    }

    [Fact]
    public async Task returns_ok_when_successful()
    {
        var dispatcher = new FakeDispatcher();
        var module = new UserModule(dispatcher);

        var result = await DeleteUserEndpoint.Handle(Example.String, module, CancellationToken.None);

        Assert.IsType<Ok>(result);
    }

    [Fact]
    public async Task returns_conflict_when_stream_does_not_exist()
    {
        var dispatcher = new FakeDispatcher();
        var module = new UserModule(dispatcher);
        dispatcher.Throws(new StreamDoesNotExistException());

        var result = await DeleteUserEndpoint.Handle(Example.String, module, CancellationToken.None);

        Assert.IsType<Conflict>(result);
    }
}
