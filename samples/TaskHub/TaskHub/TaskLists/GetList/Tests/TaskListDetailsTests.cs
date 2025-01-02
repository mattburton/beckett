using TaskHub.TaskLists.Events;

namespace TaskHub.TaskLists.GetList.Tests;

public class TaskListDetailsTests : StateSpecificationFixture<TaskListDetails>
{
    [Fact]
    public void task_list_added()
    {
        var taskListId = Guid.NewGuid();
        var name = Guid.NewGuid().ToString();

        Specification.Given(new TaskListAdded(taskListId, name)).Then(
            new TaskListDetails
            {
                Id = taskListId,
                Name = name
            }
        );
    }

    [Fact]
    public void task_added()
    {
        var taskListId = Guid.NewGuid();
        var name = Guid.NewGuid().ToString();
        var task = Guid.NewGuid().ToString();

        Specification
            .Given(
                new TaskListAdded(taskListId, name),
                new TaskAdded(taskListId, task)
            )
            .Then(
                new TaskListDetails
                {
                    Id = taskListId,
                    Name = name,
                    Tasks =
                    [
                        new TaskListDetails.TaskItem(task, false)
                    ]
                }
            );
    }

    [Fact]
    public void multiple_tasks_added()
    {
        var taskListId = Guid.NewGuid();
        var name = Guid.NewGuid().ToString();
        var task1 = Guid.NewGuid().ToString();
        var task2 = Guid.NewGuid().ToString();
        var task3 = Guid.NewGuid().ToString();

        Specification
            .Given(
                new TaskListAdded(taskListId, name),
                new TaskAdded(taskListId, task1),
                new TaskAdded(taskListId, task2),
                new TaskAdded(taskListId, task3)
            )
            .Then(
                new TaskListDetails
                {
                    Id = taskListId,
                    Name = name,
                    Tasks =
                    [
                        new TaskListDetails.TaskItem(task1, false),
                        new TaskListDetails.TaskItem(task2, false),
                        new TaskListDetails.TaskItem(task3, false)
                    ]
                }
            );
    }

    [Fact]
    public void task_completed()
    {
        var taskListId = Guid.NewGuid();
        var name = Guid.NewGuid().ToString();
        var task = Guid.NewGuid().ToString();

        Specification
            .Given(
                new TaskListAdded(taskListId, name),
                new TaskAdded(taskListId, task),
                new TaskCompleted(taskListId, task)
            )
            .Then(
                new TaskListDetails
                {
                    Id = taskListId,
                    Name = name,
                    Tasks =
                    [
                        new TaskListDetails.TaskItem(task, true)
                    ]
                }
            );
    }
}
