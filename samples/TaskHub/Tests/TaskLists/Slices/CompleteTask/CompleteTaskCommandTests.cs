using TaskHub.TaskLists.Events;
using TaskHub.TaskLists.Slices.CompleteTask;

namespace Tests.TaskLists.Slices.CompleteTask;

public class CompleteTaskCommandTests : CommandSpecificationFixture<CompleteTaskCommand, CompleteTaskCommand.State>
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
