using TaskHub.TaskList.AddTask;
using TaskHub.TaskList.Events;

namespace Tests.TaskList.AddTask;

public class AddTaskCommandTests : CommandSpecificationFixture<AddTaskCommand, AddTaskCommand.State>
{
    [Fact]
    public void task_added()
    {
        var taskListId = Guid.NewGuid();
        var task = Guid.NewGuid().ToString();

        Specification
            .When(new AddTaskCommand(taskListId, task))
            .Then(new TaskAdded(taskListId, task));
    }

    [Fact]
    public void failure_when_task_already_added()
    {
        var taskListId = Guid.NewGuid();
        var task = Guid.NewGuid().ToString();

        Specification
            .Given(new TaskAdded(taskListId, task))
            .When(new AddTaskCommand(taskListId, task))
            .Throws<TaskAlreadyAddedException>();
    }
}
