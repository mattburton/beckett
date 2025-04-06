using TaskLists.AddList;

namespace TaskLists.Tests.AddList;

public class AddListEndpointTests
{
    [Fact]
    public async Task executes_command()
    {
        var module = new FakeTaskListModule();
        var expectedCommand = new AddListCommand(Example.Guid, Example.String);
        var request = new AddListEndpoint.Request(Example.Guid, Example.String);

        await AddListEndpoint.Handle(request, module, CancellationToken.None);

        var actualCommand = Assert.IsType<AddListCommand>(module.Received);
        Assert.Equal(expectedCommand, actualCommand);
    }

    [Fact]
    public async Task returns_ok_with_result_when_successful()
    {
        var module = new FakeTaskListModule();
        var request = new AddListEndpoint.Request(Example.Guid, Example.String);

        var result = await AddListEndpoint.Handle(request, module, CancellationToken.None);

        var response = Assert.IsType<Ok<AddListEndpoint.Response>>(result);
        Assert.NotNull(response.Value);
        Assert.Equal(Example.Guid, response.Value.Id);
        Assert.Equal(Example.String, response.Value.Name);
    }

    [Fact]
    public async Task returns_conflict_when_task_list_already_exists()
    {
        var module = new FakeTaskListModule();
        var request = new AddListEndpoint.Request(Example.Guid, Example.String);
        module.Throws(new StreamAlreadyExistsException());

        var result = await AddListEndpoint.Handle(request, module, CancellationToken.None);

        Assert.IsType<Conflict>(result);
    }
}
