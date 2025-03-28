using TaskHub.TaskLists.Events;
using TaskHub.TaskLists.Queries.GetTaskLists;

namespace Tests.TaskLists.Queries.GetTaskLists;

public class TaskListsReadModelTests : ReadModelFixture<TaskListsReadModel>
{
    [Fact]
    public void task_list_added()
    {
        var taskListId = Generate.Guid();
        var name = Generate.String();

        Specification
            .Given(
                new TaskListAdded(taskListId, name)
            )
            .Then(
                new TaskListsReadModel
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
        var newName = Guid.NewGuid().ToString();

        Specification
            .Given(
                new TaskListAdded(taskListId, name),
                new TaskListNameChanged(taskListId, newName)
            )
            .Then(
                new TaskListsReadModel
                {
                    Id = taskListId,
                    Name = newName
                }
            );
    }
}
