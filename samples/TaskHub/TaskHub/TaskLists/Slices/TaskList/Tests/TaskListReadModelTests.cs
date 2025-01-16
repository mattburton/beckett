using TaskHub.TaskLists.Events;

namespace TaskHub.TaskLists.Slices.TaskList.Tests;

public class TaskListReadModelTests : StateSpecificationFixture<TaskListReadModel>
{
    [Fact]
    public void task_list_added()
    {
        var taskListId = Generate.Guid();
        var name = Generate.String();

        Specification
            .Given(
                new TaskListAdded(taskListId, name)
            ).Then(
                new TaskListReadModel
                {
                    Id = taskListId,
                    Name = name
                }
            );
    }

    [Fact]
    public void task_list_name_changed()
    {
        var taskListId = Generate.Guid();
        var name = Generate.String();
        var newName = Generate.String();

        Specification
            .Given(
                new TaskListAdded(taskListId, name),
                new TaskListNameChanged(taskListId, newName)
            )
            .Then(
                new TaskListReadModel
                {
                    Id = taskListId,
                    Name = newName,
                    Tasks = []
                }
            );
    }

    [Fact]
    public void task_added()
    {
        var taskListId = Generate.Guid();
        var name = Generate.String();
        var task = Generate.String();

        Specification
            .Given(
                new TaskListAdded(taskListId, name),
                new TaskAdded(taskListId, task)
            )
            .Then(
                new TaskListReadModel
                {
                    Id = taskListId,
                    Name = name,
                    Tasks =
                    [
                        new TaskListReadModel.TaskItem(task, false)
                    ]
                }
            );
    }

    [Fact]
    public void multiple_tasks_added()
    {
        var taskListId = Generate.Guid();
        var name = Generate.String();
        var task1 = Generate.String();
        var task2 = Generate.String();
        var task3 = Generate.String();

        Specification
            .Given(
                new TaskListAdded(taskListId, name),
                new TaskAdded(taskListId, task1),
                new TaskAdded(taskListId, task2),
                new TaskAdded(taskListId, task3)
            )
            .Then(
                new TaskListReadModel
                {
                    Id = taskListId,
                    Name = name,
                    Tasks =
                    [
                        new TaskListReadModel.TaskItem(task1, false),
                        new TaskListReadModel.TaskItem(task2, false),
                        new TaskListReadModel.TaskItem(task3, false)
                    ]
                }
            );
    }

    [Fact]
    public void task_completed()
    {
        var taskListId = Generate.Guid();
        var name = Generate.String();
        var task = Generate.String();

        Specification
            .Given(
                new TaskListAdded(taskListId, name),
                new TaskAdded(taskListId, task),
                new TaskCompleted(taskListId, task)
            )
            .Then(
                new TaskListReadModel
                {
                    Id = taskListId,
                    Name = name,
                    Tasks =
                    [
                        new TaskListReadModel.TaskItem(task, true)
                    ]
                }
            );
    }
}
