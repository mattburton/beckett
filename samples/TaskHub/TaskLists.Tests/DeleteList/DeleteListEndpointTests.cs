using TaskLists.DeleteList;

namespace TaskLists.Tests.DeleteList;

public class DeleteListEndpointTests
{
    [Fact]
    public async Task executes_command()
    {
        var expectedCommand = new DeleteListCommand(Example.Guid);
        var module = new FakeTaskListModule();

        await DeleteListEndpoint.Handle(Example.Guid, module, CancellationToken.None);

        var actualCommand = Assert.IsType<DeleteListCommand>(module.Received);
        Assert.Equal(expectedCommand, actualCommand);
    }

    [Fact]
    public async Task returns_ok_when_successful()
    {
        var module = new FakeTaskListModule();

        var result = await DeleteListEndpoint.Handle(Example.Guid, module, CancellationToken.None);

        Assert.IsType<Ok>(result);
    }

    [Fact]
    public async Task returns_conflict_when_task_list_does_not_exist()
    {
        var module = new FakeTaskListModule();
        module.Throws(new StreamDoesNotExistException());

        var result = await DeleteListEndpoint.Handle(Example.Guid, module, CancellationToken.None);

        Assert.IsType<Conflict>(result);
    }
}
