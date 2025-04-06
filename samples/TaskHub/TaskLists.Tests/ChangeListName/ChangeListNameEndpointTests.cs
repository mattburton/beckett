using TaskLists.ChangeListName;

namespace TaskLists.Tests.ChangeListName;

public class ChangeListNameEndpointTests
{
    [Fact]
    public async Task executes_command()
    {
        var expectedCommand = new ChangeListNameCommand(Example.Guid, Example.String);
        var module = new FakeTaskListModule();
        var request = new ChangeListNameEndpoint.Request(Example.String);

        await ChangeListNameEndpoint.Handle(Example.Guid, request, module, CancellationToken.None);

        var actualCommand = Assert.IsType<ChangeListNameCommand>(module.Received);
        Assert.Equal(expectedCommand, actualCommand);
    }

    [Fact]
    public async Task returns_ok_with_result_when_successful()
    {
        var module = new FakeTaskListModule();
        var request = new ChangeListNameEndpoint.Request(Example.String);

        var result = await ChangeListNameEndpoint.Handle(Example.Guid, request, module, CancellationToken.None);

        var response = Assert.IsType<Ok<ChangeListNameEndpoint.Response>>(result);
        Assert.NotNull(response.Value);
        Assert.Equal(Example.Guid, response.Value.Id);
        Assert.Equal(Example.String, response.Value.Name);
    }
}
