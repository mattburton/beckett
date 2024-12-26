using TaskHub.TaskList.CompleteTask;
using TaskHub.TaskList.Events;

namespace Tests.TaskList.CompleteTask;

public class CompleteTaskCommandTests : CommandSpecificationFixture<CompleteTaskCommand, CompleteTaskCommand.State>
{
    [Fact]
    public void task_completed()
    {
        var taskListId = Guid.NewGuid();
        var task = Guid.NewGuid().ToString();

        Specification
            .When(new CompleteTaskCommand(taskListId, task))
            .Then(new TaskCompleted(taskListId, task));
    }

    [Fact]
    public void fails_when_task_already_completed()
    {
        var taskListId = Guid.NewGuid();
        var task = Guid.NewGuid().ToString();

        Specification
            .Given(new TaskCompleted(taskListId, task))
            .When(new CompleteTaskCommand(taskListId, task))
            .Throws<TaskAlreadyCompletedException>();
    }
}
