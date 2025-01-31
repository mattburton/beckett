namespace TaskHub.TaskLists.Slices.AddTaskList.Tests;

public class AddTaskListEndpointTests
{
    [Fact]
    public async Task executes_command()
    {
        var id = Generate.Guid();
        var name = Generate.String();
        var expectedStreamName = TaskListModule.StreamName(id);
        var expectedCommand = new AddTaskListCommand(id, name);
        var expectedVersion = ExpectedVersion.StreamDoesNotExist;
        var commandBus = new FakeCommandBus();
        var request = new AddTaskListEndpoint.Request(id, name);

        await AddTaskListEndpoint.Handle(request, commandBus, CancellationToken.None);

        var actualCommand = Assert.IsType<AddTaskListCommand>(commandBus.Received);
        Assert.Equal(expectedStreamName, actualCommand.StreamName());
        Assert.Equal(expectedCommand, actualCommand);
        Assert.Equal(expectedVersion, actualCommand.ExpectedVersion);
    }

    [Fact]
    public async Task returns_ok_with_result_when_successful()
    {
        var id = Generate.Guid();
        var name = Generate.String();
        var commandBus = new FakeCommandBus();
        var request = new AddTaskListEndpoint.Request(id, name);

        var result = await AddTaskListEndpoint.Handle(request, commandBus, CancellationToken.None);

        var response = Assert.IsType<Ok<AddTaskListEndpoint.Response>>(result);
        Assert.NotNull(response.Value);
        Assert.Equal(id, response.Value.Id);
        Assert.Equal(name, response.Value.Name);
    }

    [Fact]
    public async Task returns_conflict_when_stream_already_exists()
    {
        var id = Generate.Guid();
        var name = Generate.String();
        var commandBus = new FakeCommandBus();
        var request = new AddTaskListEndpoint.Request(id, name);
        commandBus.Throws(new StreamAlreadyExistsException());

        var result = await AddTaskListEndpoint.Handle(request, commandBus, CancellationToken.None);

        Assert.IsType<Conflict>(result);
    }
}
