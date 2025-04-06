using Users.DeleteUser;

namespace Users.Tests.DeleteUser;

public class DeleteUserEndpointTests
{
    [Fact]
    public async Task executes_command()
    {
        var expectedCommand = new DeleteUserCommand(Example.String);
        var module = new FakeUserModule();

        await DeleteUserEndpoint.Handle(Example.String, module, CancellationToken.None);

        var actualCommand = Assert.IsType<DeleteUserCommand>(module.Received);
        Assert.Equal(expectedCommand, actualCommand);
    }

    [Fact]
    public async Task returns_ok_when_successful()
    {
        var module = new FakeUserModule();

        var result = await DeleteUserEndpoint.Handle(Example.String, module, CancellationToken.None);

        Assert.IsType<Ok>(result);
    }

    [Fact]
    public async Task returns_conflict_when_stream_does_not_exist()
    {
        var module = new FakeUserModule();
        module.Throws(new StreamDoesNotExistException());

        var result = await DeleteUserEndpoint.Handle(Example.String, module, CancellationToken.None);

        Assert.IsType<Conflict>(result);
    }
}
