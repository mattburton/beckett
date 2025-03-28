using API.V1.TaskLists;
using Contracts.TaskLists.Commands;
using Contracts.TaskLists.Exceptions;
using TaskHub.TaskLists;

namespace Tests.API.V1.TaskLists;

public class CompleteTaskEndpointTests
{
    [Fact]
    public async Task executes_command()
    {
        var taskListId = Generate.Guid();
        var task = Generate.String();
        var expectedCommand = new CompleteTaskCommand(taskListId, task);
        var commandDispatcher = new FakeCommandDispatcher();
        var queryDispatcher = new FakeQueryDispatcher();
        var module = new TaskListModule(commandDispatcher, queryDispatcher);

        await CompleteTaskEndpoint.Handle(taskListId, task, module, CancellationToken.None);

        var actualCommand = Assert.IsType<CompleteTaskCommand>(commandDispatcher.Received);
        Assert.Equal(expectedCommand, actualCommand);
    }

    [Fact]
    public async Task returns_ok_with_result_when_successful()
    {
        var taskListId = Generate.Guid();
        var task = Generate.String();
        var commandDispatcher = new FakeCommandDispatcher();
        var queryDispatcher = new FakeQueryDispatcher();
        var module = new TaskListModule(commandDispatcher, queryDispatcher);

        var result = await CompleteTaskEndpoint.Handle(taskListId, task, module, CancellationToken.None);

        var response = Assert.IsType<Ok<CompleteTaskEndpoint.Response>>(result);
        Assert.NotNull(response.Value);
        Assert.Equal(taskListId, response.Value.TaskListId);
        Assert.Equal(task, response.Value.Task);
    }

    [Fact]
    public async Task returns_conflict_when_task_already_added()
    {
        var taskListId = Generate.Guid();
        var task = Generate.String();
        var commandDispatcher = new FakeCommandDispatcher();
        var queryDispatcher = new FakeQueryDispatcher();
        var module = new TaskListModule(commandDispatcher, queryDispatcher);
        commandDispatcher.Throws(new TaskAlreadyCompletedException());

        var result = await CompleteTaskEndpoint.Handle(taskListId, task, module, CancellationToken.None);

        Assert.IsType<Conflict>(result);
    }
}
