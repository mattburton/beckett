using TaskHub.TaskLists.Events;

namespace TaskHub.TaskLists.Slices.GetList.Tests;

public class GetListReadModelTests : StateSpecificationFixture<GetListReadModel>
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
                new GetListReadModel
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
                new GetListReadModel
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
                new GetListReadModel
                {
                    Id = taskListId,
                    Name = name,
                    Tasks =
                    [
                        new GetListReadModel.TaskItem(task, false)
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
                new GetListReadModel
                {
                    Id = taskListId,
                    Name = name,
                    Tasks =
                    [
                        new GetListReadModel.TaskItem(task1, false),
                        new GetListReadModel.TaskItem(task2, false),
                        new GetListReadModel.TaskItem(task3, false)
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
                new GetListReadModel
                {
                    Id = taskListId,
                    Name = name,
                    Tasks =
                    [
                        new GetListReadModel.TaskItem(task, true)
                    ]
                }
            );
    }
}
