using Contracts.TaskLists.Commands;
using Contracts.TaskLists.Exceptions;
using TaskHub.TaskLists.Commands;
using TaskHub.TaskLists.Events;

namespace Tests.TaskLists.Commands;

public class CompleteTaskCommandHandlerTests :
    CommandHandlerFixture<CompleteTaskCommand, CompleteTaskCommandHandler, CompleteTaskCommandHandler.State>
{
    [Fact]
    public void task_completed()
    {
        var taskListId = Generate.Guid();
        var task = Generate.String();

        Specification
            .When(
                new CompleteTaskCommand(taskListId, task)
            )
            .Then(
                new TaskCompleted(taskListId, task)
            );
    }

    [Fact]
    public void error_when_task_already_completed()
    {
        var taskListId = Generate.Guid();
        var task = Generate.String();

        Specification
            .Given(
                new TaskCompleted(taskListId, task)
            )
            .When(
                new CompleteTaskCommand(taskListId, task)
            )
            .Throws<TaskAlreadyCompletedException>();
    }
}
