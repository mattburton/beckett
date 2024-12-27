using TaskHub.TaskList.Events;
using TaskHub.TaskList.GetList;

namespace Tests.TaskList.GetList;

public class GetListReadModelTests : StateSpecificationFixture<GetListReadModel>
{
    [Fact]
    public void task_list_added()
    {
        var taskListId = Guid.NewGuid();
        var name = Guid.NewGuid().ToString();

        Specification.Given(new TaskListAdded(taskListId, name)).Then(
            new GetListReadModel
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