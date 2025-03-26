using TaskHub.TaskLists.Events;
using TaskHub.TaskLists.Slices.AddTask;

namespace Tests.TaskLists.Slices.AddTask;

public class AddTaskCommandTests : CommandFixture<AddTaskCommand, AddTaskCommand.State>
{
    [Fact]
    public void task_added()
    {
        var taskListId = Generate.Guid();
        var task = Generate.String();

        Specification
            .When(
                new AddTaskCommand(taskListId, task)
            )
            .Then(
                new TaskAdded(taskListId, task)
            );
    }

    [Fact]
    public void user_mentioned_in_task()
    {
        var taskListId = Generate.Guid();
        const string username = nameof(username);
        const string task = $"task @{username}";

        Specification
            .When(
                new AddTaskCommand(taskListId, task)
            )
            .Then(
                new TaskAdded(taskListId, task),
                new UserMentionedInTask(taskListId, task, username)
            );
    }

    [Fact]
    public void error_when_task_already_added()
    {
        var taskListId = Generate.Guid();
        var task = Generate.String();

        Specification
            .Given(
                new TaskAdded(taskListId, task)
            )
            .When(
                new AddTaskCommand(taskListId, task)
            )
            .Throws<TaskAlreadyAddedException>();
    }
}
