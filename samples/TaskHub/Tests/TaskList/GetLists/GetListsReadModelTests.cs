using TaskHub.TaskList.Events;
using TaskHub.TaskList.GetLists;

namespace Tests.TaskList.GetLists;

public class GetListsReadModelTests : StateSpecificationFixture<GetListsReadModel>
{
    [Fact]
    public void task_list_added()
    {
        var taskListId = Guid.NewGuid();
        var name = Guid.NewGuid().ToString();

        Specification
            .Given(new TaskListAdded(taskListId, name))
            .Then(new GetListsReadModel
            {
                Id = taskListId,
                Name = name
            });
    }

    [Fact]
    public void task_list_name_changed()
    {
        var taskListId = Guid.NewGuid();
        var name = Guid.NewGuid().ToString();
        var newName = Guid.NewGuid().ToString();

        Specification
            .Given(
                new TaskListAdded(taskListId, name),
                new TaskListNameChanged(taskListId, newName)
            )
            .Then(new GetListsReadModel
            {
                Id = taskListId,
                Name = newName
            });
    }
}
